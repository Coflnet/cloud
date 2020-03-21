using System.Collections;
using System.Collections.Generic;
using unity.libsodium;

namespace Coflnet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;



    /// <summary>
    /// Base class for differentcipher suites
    /// </summary>
    public abstract class IEncryption
	{
		protected byte[] sessionSendKey;
		protected byte[] sessionReceiveKey;

		/// <summary>
		/// Message send index used for nonce
		/// </summary>
		protected long index;

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

		/// <summary>
		/// Validates a signature for given data
		/// </summary>
		/// <returns><c>true</c>, if vertify was successful, <c>false</c> otherwise.</returns>
		/// <param name="signature">Signature.</param>
		/// <param name="data">Data.</param>
		/// <param name="publicKey">Public key.</param>
		public abstract bool VertifySignatureDetached(byte[] signature, byte[] data, byte[] publicKey);

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
				if(item != null)
					length += item.Length;
			}
			byte[] newArray = new byte[length];

			int startIndex = 0;
			foreach (var item in arrays)
			{
				if(item == null)
					continue;
					
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



	namespace Extentions
	{
		public static class ByteExtentions
		{
			/// <summary>
			/// Appends byte arrays to one another
			/// </summary>
			/// <param name="target"></param>
			/// <param name="otherOnes"></param>
			/// <returns></returns>
			public static byte[] Append(this byte[] target, params byte[] otherOnes)
			{
				return IEncryption.ConcatBytes(target,otherOnes);
			}
		}
	}



	public class LibsodiumEncryption : IEncryption
	{


		/// <summary>
		/// An instance of the <see cref="LibsodiumEncryption"/> class since usually only one is required.
		/// </summary>
		public static LibsodiumEncryption Instance;
		static LibsodiumEncryption () {
			Instance = new LibsodiumEncryption ();
		}



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
		public override bool VertifySignatureDetached(byte[] signature, byte[] data, byte[] publicKey)
		{
			// skip the first byte at is only the indicator to the signature type
			return LibsodiumEncryption.SignVertifyDetached(signature.Skip(1).ToArray(),data,publicKey);
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
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

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