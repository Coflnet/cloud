using System.Collections;
using System.Collections.Generic;
using unity.libsodium;
using System.Runtime.Serialization;

namespace Coflnet
{
	using System;
	using System.Collections.Generic;

	public abstract class IEncryption
	{
		protected byte[] sessionSendKey;
		protected byte[] sessionReceiveKey;
		protected KeyPair ownKeyPair;
		/// <summary>
		/// Message send index used for nonce
		/// </summary>
		protected long index;
		/// <summary>
		/// Wherether or not the local instance is the server
		/// </summary>
		protected bool isServer;

		public long GetNextNonce()
		{
			return index++;
		}

		public byte[] GetNextNonceBytes()
		{
			return BitConverter.GetBytes(GetNextNonce());
		}

		public byte[] UniqueNonce(int length = 24)
		{
			byte[] timestampBytes = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
			byte[] indexBytes = GetNextNonceBytes();
			int reachedLength = timestampBytes.Length + indexBytes.Length;

			byte[] padding = new byte[length - reachedLength];

			return ConcatBytes(padding, timestampBytes, indexBytes);
		}

		/// <summary>
		/// Encrypts data with session key.
		/// </summary>
		/// <param name="data">Data which to encrypt</param>
		public byte[] EncryptWithSessionKey(byte[] data)
		{
			return Encrypt(data, sessionSendKey);
		}

		/// <summary>
		/// Decrypts data with session key.
		/// </summary>
		/// <param name="data">Data which to decrypt</param>
		public byte[] DecryptWithSessionKey(byte[] data)
		{
			return Decrypt(data, sessionReceiveKey);
		}

		/// <summary>
		/// Encrypt the specified data, with given key and nonce.
		/// If no nonce is given one should be generated.
		/// </summary>
		/// <param name="data">Data.</param>
		/// <param name="key">Key.</param>
		/// <param name="nonce">Nonce.</param>
		public abstract byte[] Encrypt(byte[] data, byte[] key, byte[] nonce = null);

		/// <summary>
		/// Decrypt the specified data, with given key and nonce.
		/// If no nonce is given the ciphertext contains the nonce
		/// </summary>
		/// <param name="ciphertext">Ciphertext</param>
		/// <param name="key">Key.</param>
		/// <param name="nonce">Nonce.</param>
		public abstract byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] nonce = null);

		/*
         * Removed because of ssl
         * 
		public abstract SecureConnectionServerSetupMessage GetSetupDataServer();


		public SecureConnectionClientSetupMessage ReceiveSetupDataClient(SecureConnectionServerSetupMessage serverSetupMessage)
		{
			return GetSetupDataClient(serverSetupMessage);
		}

		public abstract SecureConnectionClientSetupMessage GetSetupDataClient(SecureConnectionServerSetupMessage serverSetupMessage);


		/// <summary>
		/// Receives the setup data server.
		/// Should do the following
		/// The server now generates the session send and receive keys as well, decrypts the `server_id`, looks up the servers public key,
		/// validates that the  `public_ephermeral_key` signature is the value in `signature`, stores the session keys and destroys the setup message.
		/// </summary>
		/// <returns>The setup data server.</returns>
		/// <param name="clientSetup">Data sent by the client.</param>
		public abstract SecureConnectionServerConfirmMessage ReceiveSetupDataServer(SecureConnectionClientSetupMessage clientSetup);
        */

		public abstract void DeriveServerSessionKeys(byte[] data, KeyPair ownKeyPair);

		public abstract void DeriveClientSessionKeys(byte[] data, KeyPair ownKeyPair);

		/// <summary>
		/// A hash function to produce hashes of large amounts of data
		/// </summary>
		/// <param name="data">The data to hash.</param>
		/// <param name="hashLength">The length to short the result to.</param>
		public static byte[] Hash(byte[] data, int hashLength = 16)
		{
			throw new NotImplementedException("A hash function has to be implemented");
		}

		/// <summary>
		/// Should return sudo random bytes
		/// </summary>
		/// <returns>The generated random bytes.</returns>
		/// <param name="length">How bytes are needed</param>
		public abstract byte[] GenerateRandomBytes(int length = 16);


		public static byte[] ConcatBytes(params byte[][] arrays)
		{
			if (arrays == null)
			{
				throw new Exception("Can't concat bytes, first array is null");
			}
			int length = 0;
			foreach (var item in arrays)
			{
				length += item.Length;
			}
			byte[] newArray = new byte[length];

			int startIndex = 0;
			foreach (var item in arrays)
			{
				for (int i = 0; i < item.Length; i++)
				{
					newArray[startIndex] = item[i];
					startIndex++;
				}
			}
			return newArray;
		}

		/// <summary>
		/// Are the send and receive keys available
		/// </summary>
		/// <returns><c>true</c>, if encryption keys are set, <c>false</c> otherwise.</returns>
		public bool HasEncryptionKeys()
		{
			return sessionSendKey == null || sessionReceiveKey == null;
		}

		/// <summary>
		/// Compares two byte arrays, usually nonces if the first is bigger than the second
		/// </summary>
		/// <returns><c>true</c>, if first arguments was greater than the second, <c>false</c> otherwise.</returns>
		/// <param name="suspectedBigger">Suspected bigger.</param>
		/// <param name="second">Second.</param>
		public static bool IsLarger(byte[] suspectedBigger, byte[] second)
		{
			if (suspectedBigger.Length != second.Length)
			{
				throw new Exception("Both arrays have to have the same length");
			}
			for (int i = 0; i < suspectedBigger.Length; i++)
			{
				if (suspectedBigger[i] > second[i])
					return true;
			}
			return false;
		}
	}

	public class CoflnetEncryption : IEncryption
	{

		public const int X_NONC_ESIZE = 24;
		public const int CRYPTO_SIGN_PUBLICKEYBYTES = 32;
		public const int CRYPTO_SIGN_SECRETKEYBYTES = 64;
		public const int CRYPTO_SIGN_BYTES = 64;
		public const int CRYPTO_KX_KEYBYTES = 32;

		/// <summary>
		/// Decrypt the specified ciphertext, with key and nonce.
		/// Uses  xchacha20poly1305_ietf_decrypt
		/// </summary>
		/// <returns>The decrypt.</returns>
		/// <param name="ciphertext">Ciphertext.</param>
		/// <param name="key">Key.</param>
		/// <param name="nonce">Nonce.</param>
		public override byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] nonce = null)
		{
			byte[] message;
			if (nonce == null)
			{
				nonce = GetNonce(ciphertext);
				ciphertext = GetCipherText(ciphertext);
			}

			message = new byte[ciphertext.Length];
			long messageLength = ciphertext.Length;

			int result = NativeLibsodium.crypto_aead_xchacha20poly1305_ietf_decrypt(
				message,
				out messageLength,
				null,
				ciphertext,
				ciphertext.Length,
				null, 0,
				nonce,
				key);

			if (result != 0)
				throw new CoflnetException("decryption_error", "Decryption error");

			return message;
		}

		public override void DeriveClientSessionKeys(byte[] data, KeyPair ownKeyPair)
		{
			NativeLibsodium.crypto_kx_client_session_keys(sessionReceiveKey, sessionSendKey, ownKeyPair.publicKey, ownKeyPair.secretKey, data);
		}

		public override void DeriveServerSessionKeys(byte[] data, KeyPair ownKeyPair)
		{
			NativeLibsodium.crypto_kx_server_session_keys(sessionSendKey, sessionReceiveKey, ownKeyPair.publicKey, ownKeyPair.secretKey, data);
		}

		public override byte[] Encrypt(byte[] data, byte[] key, byte[] nonce = null)
		{
			throw new NotImplementedException();
		}

		public override byte[] GenerateRandomBytes(int length = 16)
		{
			byte[] bytes = new byte[length];
			NativeLibsodium.randombytes_buf(bytes, length);
			return bytes;
		}

		/// <summary>
		/// Hashes a given byte array with BLAKE2b (really fast).
		/// </summary>
		/// <returns>The hashed value as byte array</returns>
		/// <param name="data">Message to hash.</param>
		/// <param name="hashLength">Hash length (default 32).</param>
		public new static byte[] Hash(byte[] data, int hashLength = 32)
		{
			int messageLength = data.Length;
			byte[] hash = new byte[hashLength];
			NativeLibsodium.crypto_generichash(hash, hashLength, data, messageLength, null, 0);
			return hash;
		}

		/// <summary>
		/// Takes the first x bytes from a byte array assuming that the hash was prepended
		/// </summary>
		/// <returns>The nonce.</returns>
		/// <param name="cipherTextWithNonce">Cipher text with nonce.</param>
		public static byte[] GetNonce(byte[] cipherTextWithNonce)
		{
			byte[] nonce = new byte[X_NONC_ESIZE];
			for (int i = 0; i < X_NONC_ESIZE; i++)
			{
				nonce[i] = cipherTextWithNonce[i];
			}
			return nonce;
		}

		/// <summary>
		/// Gets the cipher text
		/// </summary>
		/// <returns>The cipher text.</returns>
		/// <param name="cipherTextWithNonce">Cipher text with nonce.</param>
		public static byte[] GetCipherText(byte[] cipherTextWithNonce)
		{
			int cipherTextLength = cipherTextWithNonce.Length - X_NONC_ESIZE;
			byte[] cipherText = new byte[cipherTextLength];
			for (int i = 0; i < cipherTextLength; i++)
			{
				cipherText[i] = cipherTextWithNonce[i + X_NONC_ESIZE];
			}
			return cipherText;
		}

		/*
		public override SecureConnectionServerSetupMessage GetSetupDataServer()
		{
			KeyPair keyPair = new KeyPair();
			var message = new SecureConnectionServerSetupMessage();
			// message.r = GenerateRandomBytes();
			message.sc = new List<string>(2);
			message.sc.Add("xchacha20poly1305");
			message.sc.Add("aes-gcm");
			//message.spsk = ServerController.instance.PuglicKeyWithSignature;
			message.sptk = SignByte(keyPair.publicKey, ServerController.instance.ServerKeys);
			return message;
		}

		public override SecureConnectionClientSetupMessage GetSetupDataClient(SecureConnectionServerSetupMessage serverSetupMessage)
		{
			// validate setup
			byte[] tempKey = ValidatePublicTempKeyAndCertificate(serverSetupMessage.certificate, serverSetupMessage.sptk);
			long singatureLength = 0;
			KeyPair empheralKeyPair = new KeyPair();
			NativeLibsodium.crypto_kx_keypair(empheralKeyPair.publicKey, empheralKeyPair.secretKey);
			DeriveClientSessionKeys(tempKey, empheralKeyPair);


			SecureConnectionClientSetupMessage clientSetupMessage = new SecureConnectionClientSetupMessage();
			clientSetupMessage.ecr = EncryptWithSessionKey(serverSetupMessage.Random);
			clientSetupMessage.pek = empheralKeyPair.publicKey;
			// set the signatue
			NativeLibsodium.crypto_sign_detached(clientSetupMessage.s, ref singatureLength, empheralKeyPair.publicKey, empheralKeyPair.publicKey.Length, ServerController.instance.ServerKeys.secretKey);

			clientSetupMessage.encid = EncryptWithSessionKey(Encoding.UTF8.GetBytes(ServerController.instance.CurrentServer.PublicId));

			return clientSetupMessage;
		}


		protected byte[] ValidatePublicTempKeyAndCertificate(CoflnetCertificate certificate, byte[] singedTempKey)
		{
			// validate the certificate
			byte[] content = Encoding.UTF8.GetBytes(MessagePackSerializer.ToJson<CoflnetCertificateContent>(certificate.content));
			byte[] hash = new byte[32];
			UIntPtr pointer = new UIntPtr();
			switch (certificate.algorythm)
			{
				case "blake2b":
					hash = Hash(content);
					break;
				case "sha256":
					NativeLibsodium.crypto_hash_sha256(hash, content, pointer);
					break;
				case "sha512":
					NativeLibsodium.crypto_hash_sha512(hash, content, pointer);
					break;
				default:
					break;
			}

			byte[] authorityPublicKey = ServerController.instance.GetAuthorityPublicKey(certificate.content.authorityName);
			if (NativeLibsodium.crypto_sign_verify_detached(certificate.signature, hash, hash.Length, authorityPublicKey) != 0)
			{
				throw new CoflnetException("invalid_certificate", "The provided certificate is invalid");
			}



			return SignOpen(singedTempKey, certificate.content.publicKey);
		}



		public override SecureConnectionServerConfirmMessage ReceiveSetupDataServer(SecureConnectionClientSetupMessage clientSetup)
		{
			var response = new SecureConnectionServerConfirmMessage();
			response.encid = Encrypt(clientSetup.encid, sessionSendKey, UniqueNonce());

			return response;
		}
*/
		/// <summary>
		/// Signs some bytes.
		/// </summary>
		/// <returns>The singed byte array.</returns>
		/// <param name="toSign">The bytes to sign</param>
		/// <param name="signKeyPair">KeyPair to sign the bytes with.</param>
		public static byte[] SignByte(byte[] toSign, KeyPair signKeyPair)
		{
			byte[] signed = new byte[toSign.Length + CRYPTO_SIGN_BYTES];
			long length = signed.Length;
			long toSingLength = toSign.Length;
			NativeLibsodium.crypto_sign(signed, ref length, toSign, toSingLength, signKeyPair.secretKey);
			return signed;
		}



		/// <summary>
		/// Signs some bytes.
		/// </summary>
		/// <returns>The singed byte array.</returns>
		/// <param name="toSign">The bytes to sign</param>
		/// <param name="signKeyPair">KeyPair to sign the bytes with.</param>
		public static byte[] SignByteDetached(byte[] toSign, KeyPair signKeyPair)
		{
			byte[] signature = new byte[CRYPTO_SIGN_BYTES];
			long signatureLength = signature.Length;
			long toSingLength = toSign.Length;
			NativeLibsodium.crypto_sign_detached(signature, ref signatureLength, toSign, toSingLength, signKeyPair.secretKey);
			return signature;
		}

		/// <summary>
		/// Validates a signature with the public part of a KeyPair
		/// </summary>
		/// <returns>The validated byte array or an Exception.</returns>
		/// <param name="signed">Signed byte array.</param>
		/// <param name="publicKey">Public key to validate the signature with.</param>
		public static byte[] SignOpen(byte[] signed, byte[] publicKey)
		{
			byte[] opened = new byte[signed.Length - CRYPTO_SIGN_BYTES];
			long length = signed.Length;
			int success = NativeLibsodium.crypto_sign_open(opened, ref length, signed, signed.Length, publicKey);
			if (success != 0)
			{
				throw new Exception("Signature is invalid");
			}
			return opened;
		}

		/// <summary>
		/// Validates a signature for given data
		/// </summary>
		/// <returns><c>true</c>, if vertify was successful, <c>false</c> otherwise.</returns>
		/// <param name="signature">Signature.</param>
		/// <param name="data">Data.</param>
		/// <param name="publicKey">Public key.</param>
		public static bool SignVertifyDetached(byte[] signature, byte[] data, byte[] publicKey)
		{
			long length = data.Length;
			int success = NativeLibsodium.crypto_sign_verify_detached(signature, data, length, publicKey);
			return success == 0;
		}
	}


	public class SecureConnectionServerSetupMessage
	{
		public static readonly int VERSION_LENGTH = 2;
		public static readonly int RANDOM_LENGTH = 16;
		public static readonly int SIGNED_PUBLIC_TEMP_KEY_LENGTH = 48;
		public static readonly int SINGED_PUBLIC_SERVER_KEY_LENGTH = 48;
		/// <summary>
		/// Gets or sets the random check value.
		/// </summary>
		/// <value>The random. (16bytes)</value>
		public byte[] Random { get; set; }
		/// <summary>
		/// Gets or sets the signed_public_temp_key.
		/// </summary>
		/// <value>The signed_public_temp_key.(48 bytes)</value>
		public byte[] sptk { get; set; }
		/// <summary>
		/// Gets or sets the singed_public_server_key.
		/// </summary>
		/// <value>The singed_public_server_key.</value>
		public CoflnetCertificate certificate { get; set; }
		/// <summary>
		/// Gets or sets the supported_ciphers.
		/// </summary>
		/// <value>The supported_ciphers.</value>
		public List<string> sc { get; set; }
		/// <summary>
		/// Gets or sets the version.
		/// </summary>
		/// <value>The version.</value>
		public byte[] v { get; set; }

		/*
        public byte[] ToBytes()
        {
            // The form is 
            byte[] serialized = IEncryption.ConcatBytes(Random, spsk, spsk, v);
            return serialized;
        }

        public SecureConnectionServerSetupMessage(byte[] data)
        {
            // try to deserialize
            Array.Copy(data, v, VERSION_LENGTH);
            Array.Copy(data, VERSION_LENGTH, sptk, 0, SIGNED_PUBLIC_TEMP_KEY_LENGTH);
            Array.Copy(data, VERSION_LENGTH + SIGNED_PUBLIC_TEMP_KEY_LENGTH, spsk, 0, SINGED_PUBLIC_SERVER_KEY_LENGTH);
            Array.Copy(data, VERSION_LENGTH + SIGNED_PUBLIC_TEMP_KEY_LENGTH + SINGED_PUBLIC_SERVER_KEY_LENGTH, Random, 0, RANDOM_LENGTH);
        }*/

		public SecureConnectionServerSetupMessage()
		{

		}
	}

	/// <summary>
	/// Coflnet certificate.
	/// </summary>
	[Obsolete("Got standardised over c# integrated ssl/tls")]
	public class CoflnetCertificate
	{
		public CoflnetCertificateContent content;
		public string algorythm;
		public byte[] signature;
	}

	public class CoflnetCertificateContent
	{
		public List<CoflnetServer.ServerRole> roles;
		public string authorityName;
		public string ip;
		public DateTime validUntil;
		public DateTime validSince;
		public byte[] publicKey;
	}



	public class SecureConnectionClientSetupMessage
	{
		/// <summary>
		/// Gets or sets the encrypted random value from the server.
		/// </summary>
		/// <value>The encrypted random value.</value>
		public byte[] ecr { get; set; }
		/// <summary>
		/// Gets or sets the public_ephermeral_key .
		/// </summary>
		/// <value>The public_ephermeral_key.</value>
		public byte[] pek { get; set; }
		/// <summary>
		/// Gets or sets the signature.
		/// </summary>
		/// <value>The signature.</value>
		public byte[] s { get; set; }
		/// <summary>
		/// Gets or sets the server_id.
		/// </summary>
		/// <value>The server_id.</value>
		public byte[] encid { get; set; }
		/// <summary>
		/// Gets or sets the picked_cipher.
		/// </summary>
		/// <value>The picked_cipher.</value>
		public string pc { get; set; }
	}

	/// <summary>
	/// Secure connection server confirm message.
	/// Sent from the server to the client.
	/// Used to validate setup.
	/// </summary>
	public class SecureConnectionServerConfirmMessage
	{
		public byte[] encid;
	}


	interface IEndToEndEncrypt
	{
		byte[] EncryptWithSessionKey(byte[] data);
		byte[] DecryptWithSessionKey(byte[] data);
	}

	[DataContract]
	public class EndToEndEncrypt
	{
		[DataContract]
		public class ChainKey
		{
			[DataMember]
			public byte[] key;
			[DataMember]
			public ulong index;
		}

		public const int X_NONC_ESIZE = 24;
		public const int CRYPTO_SIGN_PUBLICKEYBYTES = 32;
		public const int CRYPTO_SIGN_SECRETKEYBYTES = 64;
		public const int CRYPTO_SIGN_BYTES = 64;
		public const int CRYPTO_KX_KEYBYTES = 32;

		[DataMember]
		private ChainKey sessionReceiveKey;
		[DataMember]
		private ChainKey sessionSendKey;
		/// <summary>
		/// The identifier of the other side
		/// </summary>
		[DataMember]
		private SourceReference identifier;
		// we are server if we didn't send the first message
		[DataMember]
		private bool isServer;
		// has the partner received the setup information?
		[DataMember]
		private bool hasReceived;
		// "client"
		[DataMember]
		private byte[] publicIdentKey;
		[DataMember]
		private byte[] publicPreKey;
		[DataMember]
		private string oneTimeKeyIdentifier;
		[DataMember]
		private byte[] publicOneTimeKey;
		[DataMember]
		private KeyPair ephemeralKeyPair;

		// "Server" 
		[DataMember]
		private byte[] publicEphemeralKey;
		[DataMember]
		private KeyPair oneTimeKeyPair;
		[DataMember]
		private KeyPair preKeyPair;


		private static KeyPair ownIdentKeyPair;
		private static KeyPair ownPreKeyPair;
		private static KeyPair oldOwnPreKeyPair;

		/// <summary>
		/// Sets the static own keys.
		/// </summary>
		/// <param name="userKeys">User keys.</param>
		/// <param name="oldKeyPair">Old key pair.</param>
		public static void SetOwnKeys(UserKeys userKeys, UserKeys oldKeyPair)
		{
			ownIdentKeyPair = userKeys.identKey;
			ownPreKeyPair = userKeys.preKey;
			if (oldKeyPair != null)
				oldOwnPreKeyPair = oldKeyPair.identKey;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.EndToEncEncrypt"/> class.
		/// </summary>
		/// <param name="sessionReceiveKey">Session receive key.</param>
		/// <param name="sessionSendKey">Session send key.</param>
		/// <param name="identifier">Identifier of the other resource.</param>
		/// <param name="isServer">If set to <c>true</c> the current machine is the server (didn't start the connection).</param>
		/// <param name="hasReceived">If set to <c>true</c> has received.</param>
		public EndToEndEncrypt(ChainKey sessionReceiveKey, ChainKey sessionSendKey, SourceReference identifier, bool isServer, bool hasReceived)
		{
			this.sessionReceiveKey = sessionReceiveKey;
			this.sessionSendKey = sessionSendKey;
			this.identifier = identifier;
			this.isServer = isServer;
			this.hasReceived = hasReceived;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.EndToEndEncrypt"/> class.
		/// </summary>
		/// <param name="identifier">Identifier of the other side resource.</param>
		public EndToEndEncrypt(SourceReference identifier)
		{
			this.identifier = identifier;

			// TODO: generate keys
		}

		/// <summary>
		/// Creates a new signing keypair.
		/// </summary>
		/// <returns>The sign keypair.</returns>
		public static KeyPair NewSignKeypair()
		{
			KeyPair pair = new KeyPair();
			pair.publicKey = new byte[CRYPTO_SIGN_PUBLICKEYBYTES];
			pair.secretKey = new byte[CRYPTO_SIGN_SECRETKEYBYTES];
			NativeLibsodium.crypto_sign_keypair(pair.publicKey, pair.secretKey);
			return pair;
		}

		/// <summary>
		/// Generates a new kx keypair.
		/// </summary>
		/// <returns>The keypair.</returns>
		public static KeyPair NewKeypair()
		{
			byte[] publicKey = new byte[CRYPTO_KX_KEYBYTES];
			byte[] secretKey = new byte[CRYPTO_KX_KEYBYTES];
			NativeLibsodium.crypto_kx_keypair(publicKey, secretKey);
			KeyPair pair = new KeyPair
			{
				publicKey = publicKey,
				secretKey = secretKey
			};
			return pair;
		}

		/// <summary>
		/// Signs some bytes with a signing key.
		/// </summary>
		/// <returns>The signed byte.</returns>
		/// <param name="toSign">Bytes to be signed.</param>
		/// <param name="signKeyPair">Sign key pair.</param>
		public static byte[] SignByte(byte[] toSign, KeyPair signKeyPair)
		{
			byte[] signed = new byte[toSign.Length + CRYPTO_SIGN_BYTES];
			long length = signed.Length;
			long toSingLength = toSign.Length;
			NativeLibsodium.crypto_sign(signed, ref length, toSign, toSingLength, signKeyPair.secretKey);
			return signed;
		}

		/// <summary>
		/// Checks that some bytes were signed by some public key
		/// </summary>
		/// <returns>The byte open.</returns>
		/// <param name="toValidate">Bytes to validate.</param>
		/// <param name="publicSignKey">Public part of the sign key.</param>
		public static byte[] SignByteOpen(byte[] toValidate, byte[] publicSignKey)
		{
			if (toValidate.Length < CRYPTO_SIGN_BYTES)
			{
				throw new Exception($"{nameof(toValidate)} is shorter than the signature has to be.");
			}

			byte[] checkedData = new byte[toValidate.Length - CRYPTO_SIGN_BYTES];
			long length = checkedData.Length;
			long toValidateLength = toValidate.Length;
			int success = NativeLibsodium.crypto_sign_open(checkedData, ref length, toValidate, toValidateLength, publicSignKey);
			if (success == -1)
				throw new Exception("The signed bytes are not valid");
			return checkedData;
		}



		/// <summary>
		/// Decrypts the cipherText with session key.
		/// </summary>
		/// <returns>The with session key.</returns>
		/// <param name="ciphertextWithNonce">Ciphertext with nonce.</param>
		public byte[] DecryptWithSessionKey(byte[] ciphertextWithNonce)
		{
			// first 8 bytes of ciphertext are random nonce
			// then 8 bytes datetime.ticks
			// followed by the actual index
			ulong index = BitConverter.ToUInt64(ciphertextWithNonce, 16);
			return DecryptData(ciphertextWithNonce, GetSessionReceiveKey(index));
		}

		public static byte[] DecryptData(byte[] ciphertextWithNonce, byte[] key)
		{
			long messageLength = ciphertextWithNonce.Length - X_NONC_ESIZE;
			byte[] message = new byte[messageLength];
			byte[] nonce = ReadBytes(ciphertextWithNonce, 0, X_NONC_ESIZE);
			byte[] cipherText = ReadBytes(ciphertextWithNonce, X_NONC_ESIZE, ciphertextWithNonce.Length - X_NONC_ESIZE);
			int result = NativeLibsodium.crypto_aead_xchacha20poly1305_ietf_decrypt(
				message,
				out messageLength,
				null,
				cipherText,
				cipherText.Length,
				null, 0,
				nonce,
				key);

			if (result != 0)
				throw new Exception("Decryption error");

			return message;
		}

		/// <summary>
		/// Encrypts some data with some key under the use of a nonce and prepends the ciphertext with the nonce
		/// </summary>
		/// <returns>The data.</returns>
		/// <param name="data">Data.</param>
		/// <param name="key">Key.</param>
		/// <param name="nonce">Nonce.</param>
		public static byte[] EncryptData(byte[] data, byte[] key, byte[] nonce = null)
		{
			long cipherTextLength = data.Length + 16;
			byte[] cipherText = new byte[cipherTextLength];
			if (nonce == null)
				nonce = StreamEncryption.GetRandomBytes(X_NONC_ESIZE);

			int result = NativeLibsodium.crypto_aead_xchacha20poly1305_ietf_encrypt(
				cipherText,
				out cipherTextLength,
				data,
				data.Length,
				null, 0, null,
				nonce,
				key);

			if (result != 0)
				throw new Exception("Encryption error");

			byte[] cipherTextWithNonce = IEncryption.ConcatBytes(nonce, cipherText);

			return cipherTextWithNonce;
		}

		/// <summary>
		/// Encrypts data with the current session key.
		/// </summary>
		/// <returns>The encrypted data.</returns>
		/// <param name="message">Data to encrypt.</param>
		public byte[] EncryptWithSessionKey(byte[] message)
		{

			byte[] nonce = IEncryption.ConcatBytes(StreamEncryption.GetRandomBytes(X_NONC_ESIZE - 16), BitConverter.GetBytes(DateTime.UtcNow.Ticks), BitConverter.GetBytes(sessionSendKey.index));
			byte[] cipherTextWithNonce = EncryptData(message, GetCurrentSendKey(), nonce);

			// ratchat forward
			RatchetSendKey();

			return cipherTextWithNonce;
		}


		/// <summary>
		/// Derives the keys client for the client.
		/// </summary>
		/// <returns><c>true</c>, if keys client was derived, <c>false</c> otherwise.</returns>
		/// <param name="publicIdentKey">Public identification key.</param>
		/// <param name="publicPreKey">Public pre key.</param>
		/// <param name="signedOneTimeKey">Public one time key.</param>
		public bool DeriveKeysClient(byte[] publicIdentKey, byte[] publicPreKey, byte[] signedOneTimeKey)
		{
			if (publicIdentKey == null)
				throw new EncryptionKeyIsMissingException("Public IdentKey is null");
			if (publicPreKey == null)
				throw new EncryptionKeyIsMissingException("Public PreKey is null");

			this.publicIdentKey = publicIdentKey;
			this.publicPreKey = publicPreKey;


			ephemeralKeyPair = NewKeypair();

			// ephermeral And Public Pre Key
			KeyPair ePPK = new KeyPair();

			// ephermeral and Public One Time Key
			KeyPair ePOTK = new KeyPair();


			// own Pre Key and Public Pre Key
			KeyPair oPKPPK = new KeyPair();
			// own Pre Key and Public One Time Key
			KeyPair oPKPOTK = new KeyPair();

			// TODO: overgive pre key :)

			//      if (oldKeyPair) {
			//          if (publicOneTimeKey != null) {
			//              NativeLibsodium.crypto_kx_client_session_keys (ePOTK.publicKey, ePOTK.privateKey, ephemeralKeyPair.publicKey, ephemeralKeyPair.privateKey, publicOneTimeKey);
			//              NativeLibsodium.crypto_kx_client_session_keys (oPKPOTK.publicKey, oPKPOTK.privateKey, oldOwnPreKeyPair.publicKey, oldOwnPreKeyPair.privateKey, publicOneTimeKey);
			//          }
			//          NativeLibsodium.crypto_kx_client_session_keys (ePPK.publicKey, ePPK.privateKey, ephemeralKeyPair.publicKey, ephemeralKeyPair.privateKey, publicPreKey);
			//          NativeLibsodium.crypto_kx_client_session_keys (oPKPPK.publicKey, oPKPPK.privateKey, oldOwnPreKeyPair.publicKey, oldOwnPreKeyPair.privateKey, publicPreKey);
			//      } else {
			if (signedOneTimeKey != null)
			{
				// get the onetime key
				publicOneTimeKey = SignByteOpen(signedOneTimeKey, publicPreKey);

				NativeLibsodium.crypto_kx_client_session_keys(ePOTK.publicKey, ePOTK.secretKey, ephemeralKeyPair.publicKey, ephemeralKeyPair.secretKey, publicOneTimeKey);
				NativeLibsodium.crypto_kx_client_session_keys(oPKPOTK.publicKey, oPKPOTK.secretKey, ownPreKeyPair.publicKey, ownPreKeyPair.secretKey, publicOneTimeKey);
			}
			NativeLibsodium.crypto_kx_client_session_keys(ePPK.publicKey, ePPK.secretKey, ephemeralKeyPair.publicKey, ephemeralKeyPair.secretKey, publicPreKey);
			NativeLibsodium.crypto_kx_client_session_keys(oPKPPK.publicKey, oPKPPK.secretKey, ownPreKeyPair.publicKey, ownPreKeyPair.secretKey, publicPreKey);


			DeriveSessionKeys(ePPK, oPKPPK, oPKPOTK, ePOTK);
			return true;
		}

		/// <summary>
		/// Derives the session keys for the server side.
		/// </summary>
		/// <returns><c>true</c>, if keys server were derived, <c>false</c> otherwise.</returns>
		public bool DeriveKeysServer()
		{
			if (publicPreKey == null || publicEphemeralKey == null)
			{
				//return false;
				throw new EncryptionKeyIsMissingException();
			}
			// ephermeral And Public Pre Key
			KeyPair ePPK = new KeyPair();

			// ephermeral and Public One Time Key
			KeyPair ePOTK = new KeyPair();

			// own Pre Key and Public Pre Key
			KeyPair oPKPPK = new KeyPair();
			// own Pre Key and Public One Time Key
			KeyPair oPKPOTK = new KeyPair();


			if (oneTimeKeyPair != null)
			{
				NativeLibsodium.crypto_kx_server_session_keys(ePOTK.publicKey, ePOTK.secretKey, oneTimeKeyPair.publicKey, oneTimeKeyPair.secretKey, publicEphemeralKey);
				NativeLibsodium.crypto_kx_server_session_keys(oPKPOTK.publicKey, oPKPOTK.secretKey, oneTimeKeyPair.publicKey, oneTimeKeyPair.secretKey, publicPreKey);
			}
			NativeLibsodium.crypto_kx_server_session_keys(ePPK.publicKey, ePPK.secretKey, ownPreKeyPair.publicKey, ownPreKeyPair.secretKey, publicEphemeralKey);
			NativeLibsodium.crypto_kx_server_session_keys(oPKPPK.publicKey, oPKPPK.secretKey, ownPreKeyPair.publicKey, ownPreKeyPair.secretKey, publicPreKey);

			DeriveSessionKeys(ePPK, oPKPPK, oPKPOTK, ePOTK);
			return true;
		}


		/// <summary>
		/// Derives the session keys for the server.
		/// </summary>
		/// <returns><c>true</c>, if keys server was derived, <c>false</c> otherwise.</returns>
		/// <param name="publicPreKey">Public pre key.</param>
		/// <param name="publicEphemeralKey">Public ephemeral key.</param>
		/// <param name="oneTimeKeyPair">One time key pair.</param>
		public bool DeriveKeysServer(byte[] publicPreKey, byte[] publicEphemeralKey, KeyPair oneTimeKeyPair)
		{
			this.isServer = true;

			if (publicPreKey == null || publicEphemeralKey == null)
			{
				//return false;
				throw new EncryptionKeyIsMissingException();
			}
			// ephermeral And Public Pre Key
			KeyPair ePPK = new KeyPair();

			// ephermeral and Public One Time Key
			KeyPair ePOTK = new KeyPair();

			// own Pre Key and Public Pre Key
			KeyPair oPKPPK = new KeyPair();
			// own Pre Key and Public One Time Key
			KeyPair oPKPOTK = new KeyPair();


			if (oneTimeKeyPair != null)
			{
				NativeLibsodium.crypto_kx_server_session_keys(ePOTK.publicKey, ePOTK.secretKey,
															  oneTimeKeyPair.publicKey, oneTimeKeyPair.secretKey, publicEphemeralKey);
				NativeLibsodium.crypto_kx_server_session_keys(oPKPOTK.publicKey, oPKPOTK.secretKey,
															  oneTimeKeyPair.publicKey, oneTimeKeyPair.secretKey, publicPreKey);
			}
			NativeLibsodium.crypto_kx_server_session_keys(ePPK.publicKey, ePPK.secretKey,
														  ownPreKeyPair.publicKey, ownPreKeyPair.secretKey, publicEphemeralKey);
			NativeLibsodium.crypto_kx_server_session_keys(oPKPPK.publicKey, oPKPPK.secretKey,
														  ownPreKeyPair.publicKey, ownPreKeyPair.secretKey, publicPreKey);

			DeriveSessionKeys(ePPK, oPKPPK, oPKPOTK, ePOTK);
			return true;
		}

		/// <summary>
		/// Derives session keys from four key pairs and stores them in intern variables.
		/// </summary>
		/// <returns>Void.</returns>
		/// <param name="ePPK">E PP.</param>
		/// <param name="ePK">E P.</param>
		/// <param name="iPK">I P.</param>
		/// <param name="ePOTK">E POT.</param>
		private void DeriveSessionKeys(KeyPair ePPK, KeyPair ePK, KeyPair iPK, KeyPair ePOTK)
		{
			sessionReceiveKey.key = CoflnetEncryption.Hash(IEncryption.ConcatBytes(ePPK.secretKey, ePK.secretKey, iPK.secretKey, ePOTK.secretKey));
			sessionSendKey.key = CoflnetEncryption.Hash(IEncryption.ConcatBytes(ePPK.publicKey, ePK.publicKey, iPK.publicKey, ePOTK.publicKey));

			/*
            NotificationHandler.Instance.ShowNotification("",
                "sessionReceiveKey is : " + Convert.ToBase64String(sessionReceiveKey) +
                " sessionSendKey is : " + Convert.ToBase64String(sessionSendKey) +
                " ePPK s is : " + Convert.ToBase64String(ePPK.secretKey) +
                " ePK s is : " + Convert.ToBase64String(ePK.secretKey) +
                " iPK s is : " + Convert.ToBase64String(iPK.secretKey) +
                " ePOTK s is : " + Convert.ToBase64String(ePOTK.secretKey) +
                " ePPK is : " + Convert.ToBase64String(ePPK.publicKey) +
                " ePK is : " + Convert.ToBase64String(ePK.publicKey) +
                " iPK is : " + Convert.ToBase64String(iPK.publicKey) +
                " ePOTK is : " + Convert.ToBase64String(ePOTK.publicKey));*/
		}


		/// <summary>
		/// Destroies the temp keys in memory.
		/// </summary>
		/// <returns>Void.</returns>
		public void DestroyTempKeys()
		{
			if (ephemeralKeyPair != null)
			{
				SetArrayNull(ephemeralKeyPair.secretKey);
				SetArrayNull(ephemeralKeyPair.publicKey);
			}
			if (oneTimeKeyPair != null)
			{
				SetArrayNull(oneTimeKeyPair.secretKey);
				SetArrayNull(oneTimeKeyPair.publicKey);
			}
			SetArrayNull(publicOneTimeKey);
			SetArrayNull(publicEphemeralKey);
		}

		private void SetArrayNull(byte[] target)
		{
			if (target == null)
				return;
			for (int i = 0; i < target.Length; i++)
			{
				target[i] = 0x00;
			}
			target = null;
		}

		public static byte[] ReadBytes(byte[] source, int offset, int length)
		{
			var newArray = new byte[length];

			Array.Copy(source, offset, newArray, 0, length);
			return newArray;
		}

		/// <summary>
		/// Gets the current send key.
		/// </summary>
		/// <returns>The current send key.</returns>
		private byte[] GetCurrentSendKey()
		{
			return ChainToEncryptionKey(this.sessionSendKey.key);
		}

		/// <summary>
		/// Ratchets the send key.
		/// </summary>
		private void RatchetSendKey()
		{
			RatchetChainKey(sessionSendKey);
		}

		/// <summary>
		/// Gets the current receive key.
		/// </summary>
		/// <returns>The current receive key.</returns>
		private byte[] GetCurrentReceiveKey()
		{
			return ChainToEncryptionKey(sessionReceiveKey.key);
		}

		/// <summary>
		/// Converts a chain key to an Encryption key for actual usage
		/// </summary>
		/// <returns>The encryption key.</returns>
		/// <param name="chainKey">Chain key.</param>
		private byte[] ChainToEncryptionKey(byte[] chainKey)
		{
			return CoflnetEncryption.Hash(IEncryption.ConcatBytes(chainKey, new byte[] { 0x01 }));
		}

		/// <summary>
		/// Gets the session receive key for a given index.
		/// IMPORTANT: the key will be ratchet if the index is 30 higher than the current one
		/// </summary>
		/// <returns>The session receive key.</returns>
		/// <param name="index">Index.</param>
		private byte[] GetSessionReceiveKey(ulong index)
		{
			if (sessionReceiveKey == null || sessionReceiveKey.key.Length < 16)
			{
				// no session key yet :(
				//SocketController.instance.SendCommand(identifier, "resendSetup", "", "", 4, DataShifter.serverForgetTime);
				throw new Exception("session receivekey is missing");
			}

			ulong minIndex = 0;
			// check to NOT get an overflow (was a bug, took about 20 hours)
			if (index > 40)
				minIndex = index - 30;
			if (minIndex > sessionReceiveKey.index)
			{
				// the current key is to far behind, ratchet the key forward
				while (minIndex > sessionReceiveKey.index)
				{
					// this also increases the receiveKeyIndex
					RatchetReceiveKey();
				}
				//sessionReceiveKey = AdvanceKey(sessionReceiveKey, (int)(minIndex - receiveKeyIndex));
			}
			return ChainToEncryptionKey(AdvanceKey(sessionReceiveKey.key, (int)(index - sessionReceiveKey.index)));
		}


		private void RatchetReceiveKey()
		{
			RatchetChainKey(sessionReceiveKey);
		}

		/// <summary>
		/// Ratchets the a chain key.
		/// Does this by appending 0x02 to and hashing the current key
		/// And icrementing the chainkeyIndex
		/// </summary>
		/// <param name="key">Key.</param>
		public static void RatchetChainKey(ChainKey key)
		{
			lock (key)
			{
				key.key = AdvanceKey(key.key);
				key.index++;
			}
		}
		/// <summary>
		/// Gets the enc key from the chainkey.
		/// Does this by appending 0x01 to and hashing the current key 
		/// </summary>
		/// <returns>The enc key from chain.</returns>
		/// <param name="chainKey">Chain key.</param>
		public static byte[] GetEncKeyFromChain(ChainKey chainKey)
		{
			return CoflnetEncryption.Hash(IEncryption.ConcatBytes(chainKey.key, new byte[] { 0x01 }));
		}

		/// <summary>
		/// Advances the some key by some count.
		/// </summary>
		/// <returns>The key.</returns>
		/// <param name="key">Key.</param>
		/// <param name="count">Count.</param>
		public static byte[] AdvanceKey(byte[] key, int count)
		{
			byte[] tmpKey = key;
			for (int i = 0; i < count; i++)
			{
				tmpKey = AdvanceKey(key);
			}
			return tmpKey;
		}

		/// <summary>
		/// Advances the key.
		/// </summary>
		/// <returns>The key.</returns>
		/// <param name="key">Key.</param>
		public static byte[] AdvanceKey(byte[] key)
		{
			return CoflnetEncryption.Hash(IEncryption.ConcatBytes(key, new byte[] { 0x02 }));
		}


		public SourceReference Identifier
		{
			get
			{
				return identifier;
			}
		}

		public byte[] PublicIdentKey
		{
			get
			{
				return publicIdentKey;
			}
		}

		/// <summary>
		/// Gets the session setup headers.
		/// </summary>
		/// <returns>The session setup headers.</returns>
		public ChatSetupHeader GetSessionSetupHeaders()
		{
			ChatSetupHeader header = new ChatSetupHeader();
			if (ephemeralKeyPair != null)
				header.publicEphemeralKey = ephemeralKeyPair.publicKey;

			header.publicIdentKey = ownIdentKeyPair.publicKey;
			header.publicPreKey = ownPreKeyPair.publicKey;
			if (publicOneTimeKey != null)
				header.publicOneTimeKey = this.publicOneTimeKey;
			return header;
		}
	}


	public class GroupEndToEndEncryption : EndToEndEncrypt
	{
		public SourceReference GroupId;

		public GroupEndToEndEncryption(ChainKey sessionReceiveKey, ChainKey sessionSendKey, SourceReference identifier, bool isServer, bool hasReceived, SourceReference groupId) : base(sessionReceiveKey, sessionSendKey, identifier, isServer, hasReceived)
		{
			GroupId = groupId;
		}

		public GroupEndToEndEncryption(SourceReference identifier, SourceReference groupId) : base(identifier)
		{
			GroupId = groupId;
		}
	}

	public class EncryptionKeyIsMissingException : Exception
	{
		public EncryptionKeyIsMissingException() { }

		public EncryptionKeyIsMissingException(string message) : base(message)
		{

		}
	}
}