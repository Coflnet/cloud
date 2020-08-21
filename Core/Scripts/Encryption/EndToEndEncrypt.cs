using unity.libsodium;
using System.Runtime.Serialization;

namespace Coflnet
{
    using System;

    /// <summary>
    /// Represents the state of end to end encrypted libsodium channel
    /// </summary>
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
		private EntityId identifier;
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
		public EndToEndEncrypt(ChainKey sessionReceiveKey, ChainKey sessionSendKey, EntityId identifier, bool isServer, bool hasReceived)
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
		public EndToEndEncrypt(EntityId identifier)
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
			sessionReceiveKey.key = LibsodiumEncryption.Hash(IEncryption.ConcatBytes(ePPK.secretKey, ePK.secretKey, iPK.secretKey, ePOTK.secretKey));
			sessionSendKey.key = LibsodiumEncryption.Hash(IEncryption.ConcatBytes(ePPK.publicKey, ePK.publicKey, iPK.publicKey, ePOTK.publicKey));

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
			return LibsodiumEncryption.Hash(IEncryption.ConcatBytes(chainKey, new byte[] { 0x01 }));
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
			return LibsodiumEncryption.Hash(IEncryption.ConcatBytes(chainKey.key, new byte[] { 0x01 }));
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
			return LibsodiumEncryption.Hash(IEncryption.ConcatBytes(key, new byte[] { 0x02 }));
		}


		public EntityId Identifier
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
}