using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using unity.libsodium;
using System.Linq;
using MessagePack;
using Coflnet;


namespace Coflnet
{



	public class Encrypt
	{
		const string glyphs = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		private RSACryptoServiceProvider RSA;

		private string AESKey;

		/*
		void Start() {

			string message = "Nices System du ha";
			string password = "3sc3RLrpd1Ä";

			string enc = "e609b3abfb43343bKsnNtQgnPRHQIqYL1IxLYDjjaoGLRD/nYeqhnErRI5A=";

			// Create sha256 hash
			SHA256 mySHA256 = SHA256Managed.Create();
			byte[] key = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(password));

			// Create secret IV
			byte[] iv = new byte[16] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };


			string encrypted = this.EncryptString(message, key);
			string decrypted = this.DecryptString(encrypted, key);


			//Debugger.text += "key: " + key);
			Debug.Log(" iv: " + iv);
			Debug.Log("message: " + message);
			Debugger.text +="\n\nencrypted: " + encrypted;
			Debugger.text +="\ndecrypted: " + decrypted;
			//Console.ReadKey(true);   QBlgcQ2+v3wd8RLjhtu07ZBd8aQWjPMfTc/73TPzlyA=
		}
	*/
		/// <summary>
		/// Encrypts a string.
		/// </summary>
		/// <returns>The encrypted string.</returns>
		/// <param name="plainText">Plain text.</param>
		/// <param name="keyString">Key string.</param>
		public static string EncryptString(string plainText, string keyString)
		{
			byte[] key = Encoding.UTF8.GetBytes(keyString);
			// Generate an iv
			string ivString = "";
			System.Random rnd = new System.Random();
			for (int i = 0; i < 16; i++)
			{
				ivString += glyphs[rnd.Next(glyphs.Length)];
			}
			byte[] iv = Encoding.UTF8.GetBytes(ivString);

			// Instantiate a new Aes object to perform string symmetric encryption
			Aes encryptor = Aes.Create();

			encryptor.Mode = CipherMode.CBC;
			//encryptor.KeySize = 256;
			//encryptor.BlockSize = 128;
			//encryptor.Padding = PaddingMode.Zeros;

			// Set key and IV
			encryptor.Key = key;
			encryptor.IV = iv;

			// Instantiate a new MemoryStream object to contain the encrypted bytes
			MemoryStream memoryStream = new MemoryStream();

			// Instantiate a new encryptor from our Aes object
			ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();

			// Instantiate a new CryptoStream object to process the data and write it to the 
			// memory stream
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

			// Convert the plainText string into a byte array
			byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

			// Encrypt the input plaintext string
			cryptoStream.Write(plainBytes, 0, plainBytes.Length);

			// Complete the encryption process
			cryptoStream.FlushFinalBlock();

			// Convert the encrypted data from a MemoryStream to a byte array
			byte[] cipherBytes = memoryStream.ToArray();

			// Close both the MemoryStream and the CryptoStream
			memoryStream.Close();
			cryptoStream.Close();

			// Convert the encrypted byte array to a base64 encoded string
			string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

			// Return the encrypted data as a string
			return ivString + cipherText;
		}

		public static string DecryptString(string cipherText, string keyString)
		{
			// key is mostlikely a string but we need bytes
			byte[] key = Encoding.UTF8.GetBytes(keyString);

			// seperate the text from the iv
			byte[] iv = Encoding.UTF8.GetBytes(cipherText.Substring(0, 16));
			cipherText = cipherText.Substring(16);



			// Instantiate a new Aes object to perform string symmetric encryption
			Aes encryptor = Aes.Create();

			encryptor.Mode = CipherMode.CBC;
			//encryptor.KeySize = 256;
			//encryptor.BlockSize = 128;
			//encryptor.Padding = PaddingMode.Zeros;

			// Set key and IV
			encryptor.Key = key;
			encryptor.IV = iv;

			// Instantiate a new MemoryStream object to contain the encrypted bytes
			MemoryStream memoryStream = new MemoryStream();

			// Instantiate a new encryptor from our Aes object
			ICryptoTransform aesDecryptor = encryptor.CreateDecryptor();

			// Instantiate a new CryptoStream object to process the data and write it to the 
			// memory stream
			CryptoStream cryptoStream = new CryptoStream(memoryStream, aesDecryptor, CryptoStreamMode.Write);

			// Will contain decrypted plaintext
			string plainText = String.Empty;

			try
			{
				// Convert the ciphertext string into a byte array
				byte[] cipherBytes = Convert.FromBase64String(cipherText);

				// Decrypt the input ciphertext string
				cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);

				// Complete the decryption process
				cryptoStream.FlushFinalBlock();

				// Convert the decrypted data from a MemoryStream to a byte array
				byte[] plainBytes = memoryStream.ToArray();

				// Convert the encrypted byte array to a base64 encoded string
				plainText = Encoding.UTF8.GetString(plainBytes, 0, plainBytes.Length);
			}
			finally
			{
				// Close both the MemoryStream and the CryptoStream
				memoryStream.Close();
				cryptoStream.Close();
			}

			// Return the encrypted data as a string
			return plainText;
		}


		public static string MD5Hash(string input)
		{
			StringBuilder hash = new StringBuilder();
			MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
			byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

			for (int i = 0; i < bytes.Length; i++)
			{
				hash.Append(bytes[i].ToString("x2"));
			}
			return hash.ToString();
		}


		public static string MD5HashAndInsert(string input, string insert)
		{
			string hash = MD5Hash(input);
			return hash.Substring(0, 11) + insert + hash.Substring(11);
		}


		public static void FillRandomBytes(byte[] ar)
		{
			RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();
			csp.GetBytes(ar);
			return;
		}


		public static byte[] SHA256(string input)
		{
			SHA256 mySHA256 = SHA256Managed.Create();
			return mySHA256.ComputeHash(new UTF8Encoding().GetBytes(input));
		}



		public void SetKeyPair(string key)
		{
			RSA.FromXmlString(key);
		}


		public string GenerateNewKeyPair(int length = 4096)
		{
			//Generate a public/private key pair.  
			RSA = new RSACryptoServiceProvider(length);
			return RSA.ToXmlString(true);
		}

		public byte[] RSAEncrypt(string data, string xmlKey = null)
		{
			if (xmlKey != null)
			{
				RSA = ServiceProviderFromXML(xmlKey);
			}
			var dataBytes = Encoding.UTF8.GetBytes(data);

			return RSA.Encrypt(dataBytes, false);
		}

		public string EncryptBase64(string data, string xmlKey = null)
		{
			var encryptedData = RSAEncrypt(data, xmlKey);
			return System.Convert.ToBase64String(encryptedData);
		}


		public RSACryptoServiceProvider ServiceProviderFromXML(string xml)
		{
			var rsaClient = new RSACryptoServiceProvider(4096);
			rsaClient.FromXmlString(xml);
			return rsaClient;
		}


		public string RSADecrypt(byte[] data, string xmlKey = null)
		{
			if (xmlKey != null)
			{
				RSA = ServiceProviderFromXML(xmlKey);
			}
			var decryptedData = RSA.Decrypt(data, false);

			return Encoding.UTF8.GetString(decryptedData);
		}



		public string DecryptBase64(string base64, string xmlKey = null)
		{
			var unencodedData = System.Convert.FromBase64String(base64);
			return RSADecrypt(unencodedData, xmlKey);
		}

		public Encrypt(string xmlKey)
		{
			RSA = ServiceProviderFromXML(xmlKey);
		}

		public Encrypt(int length)
		{
			GenerateNewKeyPair(length);
		}


		public string GetPublicKey()
		{
			return RSA.ToXmlString(false);
		}


		public string EncryptAES(string content)
		{
			if (AESKey == null)
				throw new Exception();
			return EncryptString(content, AESKey);
		}


		public string DecryptAES(string content)
		{
			if (AESKey == null)
				throw new Exception();
			return DecryptString(content, AESKey);
		}

		public void SetAESKey(string key)
		{
			AESKey = key;
		}






		// new encryption with sodium (libsodium)
		public const int X_NONC_ESIZE = 24;
		public const int CRYPTO_SIGN_PUBLICKEYBYTES = 32;
		public const int CRYPTO_SIGN_SECRETKEYBYTES = 64;
		public const int CRYPTO_SIGN_BYTES = 64;
		public const int CRYPTO_KX_KEYBYTES = 32;

		[SerializeField]
		private byte[] sessionReceiveKey;
		[SerializeField]
		private ulong receiveKeyIndex = 0;
		[SerializeField]
		private byte[] sessionSendKey;
		[SerializeField]
		private ulong sendKeyIndex = 0;
		[SerializeField]
		private SourceReference identifier;
		// we are server if we didn't send the first message
		[SerializeField]
		private bool isServer;
		// has the partner received the setup information?
		[SerializeField]
		private bool hasReceived;
		// "client"
		[SerializeField]
		private byte[] publicIdentKey;
		[SerializeField]
		private byte[] publicPreKey;
		[SerializeField]
		private string oneTimeKeyIdentifier;
		[SerializeField]
		private byte[] publicOneTimeKey;
		[SerializeField]
		private KeyPair ephemeralKeyPair;

		// "Server" 
		[SerializeField]
		private byte[] publicEphemeralKey;
		[SerializeField]
		private KeyPair oneTimeKeyPair;
		[SerializeField]
		private KeyPair preKeyPair;
		[SerializeField]
		private static KeyPair ownIdentKeyPair;
		[SerializeField]
		private static KeyPair ownPreKeyPair;
		[SerializeField]
		private static KeyPair oldOwnPreKeyPair;


		public static void SetOwnKeys(UserKeys keypair, UserKeys oldKeyPair)
		{
			ownIdentKeyPair = keypair.identKey;
			ownPreKeyPair = keypair.preKey;
			if (oldKeyPair != null)
				oldOwnPreKeyPair = oldKeyPair.identKey;
		}



		/// <summary>
		/// Initializes a new instance of the <see cref="Encrypt"/> class.
		/// </summary>
		/// <param name="publicKey">Public key.</param>
		/// <param name="identifier">Identifier.</param>
		/// <param name="isServer">Is this instance a server (didn't make the initial request).</param>
		/// <param name="sessionReceiveKey">Session receive key.</param>
		/// <param name="sessionSendKey">Session send key.</param>
		/// <param name="publicPreKey">Public pre key.</param>
		/// <param name="ephemeralKeyPair">Ephemeral key pair.</param>
		/// <param name="publicOneTimeKey">Public one time key.</param>
		/// <param name="publicIdentKey">public ident key.</param>
		public Encrypt(byte[] publicKey, SourceReference identifier, bool isServer = false, byte[] sessionReceiveKey = null,
			byte[] sessionSendKey = null, byte[] publicPreKey = null, KeyPair ephemeralKeyPair = null, byte[] publicOneTimeKey = null, byte[] publicIdentKey = null)
		{
			this.publicIdentKey = publicKey;
			this.identifier = identifier;
			this.isServer = isServer;
			this.sessionReceiveKey = sessionReceiveKey;
			this.sessionSendKey = sessionSendKey;
			this.publicPreKey = publicPreKey;
			this.ephemeralKeyPair = ephemeralKeyPair;
			this.publicIdentKey = publicIdentKey;
		}

		public static KeyPair NewSingKeypair()
		{
			KeyPair pair = new KeyPair();
			pair.publicKey = new byte[CRYPTO_SIGN_PUBLICKEYBYTES];
			pair.secretKey = new byte[CRYPTO_SIGN_SECRETKEYBYTES];
			NativeLibsodium.crypto_sign_keypair(pair.publicKey, pair.secretKey);
			return pair;
		}

		public static byte[] SignByte(byte[] toSign, KeyPair signKeyPair)
		{
			byte[] signed = new byte[toSign.Length + CRYPTO_SIGN_BYTES];
			long length = signed.Length;
			long toSingLength = toSign.Length;
			NativeLibsodium.crypto_sign(signed, ref length, toSign, toSingLength, signKeyPair.secretKey);
			return signed;
		}


		public static void Test()
		{
			KeyPair signKeyPair = NewSingKeypair();
			byte[] toSign = Encoding.UTF8.GetBytes("test!");
			byte[] signed = new byte[toSign.Length + CRYPTO_SIGN_BYTES];
			long length = signed.Length;
			long toSingLength = toSign.Length;
			NativeLibsodium.crypto_sign(signed, ref length, toSign, toSingLength, signKeyPair.secretKey);


			byte[] checkedData = new byte[signed.Length - CRYPTO_SIGN_BYTES];
			long clength = checkedData.Length;
			long toValidateLength = signed.Length;
			NativeLibsodium.crypto_sign_open(checkedData, ref clength, signed, toValidateLength, signKeyPair.publicKey);
			Debug.Log("ahea " + Encoding.UTF8.GetString(signed));
			Debug.Log(" ahea " + Encoding.UTF8.GetString(checkedData));
		}



		public static byte[] SignByteOpen(byte[] toValidate, byte[] publicSignKey)
		{
			if (toValidate.Length < CRYPTO_SIGN_BYTES)
			{
				Debug.LogError(" toValidate isn't long enough" + Convert.ToBase64String(toValidate));
				// probably no keys left
				return null;
			}

			byte[] checkedData = new byte[toValidate.Length - CRYPTO_SIGN_BYTES];
			long length = checkedData.Length;
			long toValidateLength = toValidate.Length;
			NativeLibsodium.crypto_sign_open(checkedData, ref length, toValidate, toValidateLength, publicSignKey);
			return checkedData;
		}

		/// <summary>
		/// Generates a new keypair.
		/// </summary>
		/// <returns>The keypair.</returns>
		public static KeyPair NewKeypair()
		{
			byte[] publicKey = new byte[CRYPTO_KX_KEYBYTES];
			byte[] secretKey = new byte[CRYPTO_KX_KEYBYTES];
			NativeLibsodium.crypto_kx_keypair(publicKey, secretKey);
			KeyPair pair = new KeyPair();
			pair.publicKey = publicKey;
			pair.secretKey = secretKey;
			return pair;
		}

		public string GetOwnPublicKey()
		{
			return System.Convert.ToBase64String(publicIdentKey);
		}

		public string GetOwnPrivateKey()
		{
			return System.Convert.ToBase64String(ownIdentKeyPair.secretKey);
		}

		public string Receive(string message)
		{
			return Receive(Encoding.UTF8.GetBytes(message));
		}


		public string Receive(byte[] message)
		{
			if (sessionReceiveKey != null)
				return GetString(DecryptWithSessionKey(message));
			return "";
		}


		public string ReceiveServer(byte[] message)
		{
			return GetString(message);
		}

		public string ReceiveClient(byte[] message)
		{
			return GetString(message);
		}


		public string DecryptWithSessionKey(string cipherTextWithNonce, ulong index = 0)
		{
			return Receive(Convert.FromBase64String(cipherTextWithNonce));
		}


		public byte[] DecryptWithSessionKey(byte[] ciphertextWithNonce, ulong index = 0)
		{

			byte[] message = DecryptData(ciphertextWithNonce, GetSessionReceiveKey(index));

			// ratchat forward 
			//RatchetReceiveKey();

			// currently the GetSessionReceiveKey does the Ratchating for us :)

			return message;
		}

		public static byte[] DecryptData(byte[] ciphertextWithNonce, byte[] key)
		{
			long messageLength = ciphertextWithNonce.Length - X_NONC_ESIZE;
			byte[] message = new byte[messageLength];
			byte[] nonce = GetNonce(ciphertextWithNonce);
			byte[] cipherText = GetCipherText(ciphertextWithNonce);
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
				Debug.LogError("Decryption error");

			return message;
		}


		public static byte[] EncryptData(byte[] data, byte[] key)
		{
			long cipherTextLength = data.Length + 16;
			byte[] cipherText = new byte[cipherTextLength];
			byte[] nonce = StreamEncryption.GetRandomBytes(X_NONC_ESIZE);

			int result = NativeLibsodium.crypto_aead_xchacha20poly1305_ietf_encrypt(
				cipherText,
				out cipherTextLength,
				data,
				data.Length,
				null, 0, null,
				nonce,
				key);

			if (result != 0)
				Debug.LogError("Encryption error");

			byte[] cipherTextWithNonce = ConcatNonceAndCipherText(nonce, cipherText);

			return cipherTextWithNonce;
		}

		public byte[] EncryptWithSessionKey(byte[] message)
		{
			byte[] cipherTextWithNonce = EncryptData(message, GetCurrentSendKey());

			// ratchat forward
			RatchetSendKey();

			return cipherTextWithNonce;
		}


		public void ComputeKeysServer()
		{
			NativeLibsodium.crypto_kx_server_session_keys(sessionReceiveKey, sessionSendKey, ownIdentKeyPair.publicKey, ownIdentKeyPair.secretKey, publicIdentKey);
		}


		public void ComputeKeysClient()
		{
			NativeLibsodium.crypto_kx_client_session_keys(sessionReceiveKey, sessionSendKey, ownIdentKeyPair.publicKey, ownIdentKeyPair.secretKey, publicIdentKey);
		}

		public bool DeriveKeysClient(bool oldKeyPair = false)
		{
			if (publicIdentKey == null || publicPreKey == null)
			{
				return false;
				//throw new EncryptionKeyIsMissingException();
			}

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

			//		if (oldKeyPair) {
			//			if (publicOneTimeKey != null) {
			//				NativeLibsodium.crypto_kx_client_session_keys (ePOTK.publicKey, ePOTK.privateKey, ephemeralKeyPair.publicKey, ephemeralKeyPair.privateKey, publicOneTimeKey);
			//				NativeLibsodium.crypto_kx_client_session_keys (oPKPOTK.publicKey, oPKPOTK.privateKey, oldOwnPreKeyPair.publicKey, oldOwnPreKeyPair.privateKey, publicOneTimeKey);
			//			}
			//			NativeLibsodium.crypto_kx_client_session_keys (ePPK.publicKey, ePPK.privateKey, ephemeralKeyPair.publicKey, ephemeralKeyPair.privateKey, publicPreKey);
			//			NativeLibsodium.crypto_kx_client_session_keys (oPKPPK.publicKey, oPKPPK.privateKey, oldOwnPreKeyPair.publicKey, oldOwnPreKeyPair.privateKey, publicPreKey);
			//		} else {
			if (publicOneTimeKey != null)
			{
				NativeLibsodium.crypto_kx_client_session_keys(ePOTK.publicKey, ePOTK.secretKey, ephemeralKeyPair.publicKey, ephemeralKeyPair.secretKey, publicOneTimeKey);
				NativeLibsodium.crypto_kx_client_session_keys(oPKPOTK.publicKey, oPKPOTK.secretKey, ownPreKeyPair.publicKey, ownPreKeyPair.secretKey, publicOneTimeKey);
			}
			NativeLibsodium.crypto_kx_client_session_keys(ePPK.publicKey, ePPK.secretKey, ephemeralKeyPair.publicKey, ephemeralKeyPair.secretKey, publicPreKey);
			NativeLibsodium.crypto_kx_client_session_keys(oPKPPK.publicKey, oPKPPK.secretKey, ownPreKeyPair.publicKey, ownPreKeyPair.secretKey, publicPreKey);


			DeriveSessionKeys(ePPK, oPKPPK, oPKPOTK, ePOTK);
			return true;
		}




		public bool DeriveKeysServer()
		{
			if (publicPreKey == null || publicEphemeralKey == null)
			{
				Debug.LogError("keys are missing");
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
		/// Derives session keys from four key pairs and stores them in intern variables.
		/// </summary>
		/// <returns>Void.</returns>
		/// <param name="ePPK">E PP.</param>
		/// <param name="ePK">E P.</param>
		/// <param name="iPK">I P.</param>
		/// <param name="ePOTK">E POT.</param>
		private void DeriveSessionKeys(KeyPair ePPK, KeyPair ePK, KeyPair iPK, KeyPair ePOTK)
		{
			sessionReceiveKey = Hash(ConcatBytes(ePPK.secretKey, ePK.secretKey, iPK.secretKey, ePOTK.secretKey));
			sessionSendKey = Hash(ConcatBytes(ePPK.publicKey, ePK.publicKey, iPK.publicKey, ePOTK.publicKey));

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
			Debug.Log("DESTROYED tmp keys");
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

			//ephemeralKeyPair = null;
			//oneTimeKeyPair = null;
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




		/// <summary>
		/// Hashes a given byte array with BLAKE2b (really fast).
		/// </summary>
		/// <returns>The hashed value as byte array</returns>
		/// <param name="message">Message to hash.</param>
		/// <param name="hashLength">Hash length (default 32).</param>
		public static byte[] Hash(byte[] message, int hashLength = 32)
		{
			int messageLength = message.Length;
			byte[] hash = new byte[hashLength];
			NativeLibsodium.crypto_generichash(hash, hashLength, message, messageLength, null, 0);
			return hash;
		}


		private string GetString(byte[] message)
		{
			return Encoding.ASCII.GetString(message);
		}


		public static byte[] GetNonce(byte[] cipherTextWithNonce)
		{
			byte[] nonce = new byte[X_NONC_ESIZE];
			for (int i = 0; i < X_NONC_ESIZE; i++)
			{
				nonce[i] = cipherTextWithNonce[i];
			}
			return nonce;
		}

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

		public static byte[] ConcatNonceAndCipherText(byte[] nonce, byte[] cipherText)
		{
			if (nonce.Length != X_NONC_ESIZE)
				throw new Exception("nonce size isn't 24 bytes as required by XCHACHA20");

			return ConcatBytes(nonce, cipherText);
		}


		public static byte[] ConcatBytes(byte[] firstArray, byte[] secondArray, byte[] thirdArray = null, byte[] fourthArray = null)
		{
			if (firstArray == null)
			{
				throw new Exception("Can't concat bytes, first array is null");
			}
			if (secondArray == null)
			{
				throw new Exception("Can't concat bytes, second array is null");
			}
			int length = firstArray.Length + secondArray.Length;
			if (thirdArray != null)
				length += thirdArray.Length;
			if (fourthArray != null)
				length += fourthArray.Length;
			byte[] newArray = new byte[length];

			int startIndex = 0;
			writeInto(firstArray, newArray, startIndex);
			startIndex += firstArray.Length;
			writeInto(secondArray, newArray, startIndex);
			if (fourthArray != null)
			{
				startIndex += secondArray.Length;
				writeInto(thirdArray, newArray, startIndex);
			}
			if (fourthArray != null)
			{
				startIndex += thirdArray.Length;
				writeInto(fourthArray, newArray, startIndex);
			}
			return newArray;
		}

		private static void writeInto(byte[] toWrite, byte[] into, int index)
		{
			for (int i = 0; i < toWrite.Length; i++)
			{
				into[i + index] = toWrite[i];
			}
		}

		private byte[] GetCurrentSendKey()
		{
			Debug.Log("Chain send key is:" + JsonUtility.ToJson(sessionSendKey));

			return ChainToEncryptionKey(this.sessionSendKey);
		}

		private void RatchetSendKey()
		{
			sessionSendKey = RatchetKey(sessionSendKey);
			sendKeyIndex++;
		}

		private byte[] GetCurrentReceiveKey()
		{
			return ChainToEncryptionKey(sessionReceiveKey);
		}

		/// <summary>
		/// Converts a chain key to an Encryption key for actual usage
		/// </summary>
		/// <returns>The encryption key.</returns>
		/// <param name="chainKey">Chain key.</param>
		private byte[] ChainToEncryptionKey(byte[] chainKey)
		{
			return Hash(ConcatBytes(chainKey, new byte[] { 0x01 }));
		}

		/// <summary>
		/// Gets the session receive key for a given index.
		/// IMPORTANT: the key will be ratchet if the index is 30 higher than the current one
		/// </summary>
		/// <returns>The session receive key.</returns>
		/// <param name="index">Index.</param>
		private byte[] GetSessionReceiveKey(ulong index)
		{
			if (sessionReceiveKey == null || sessionReceiveKey.Length < 16)
			{
				// no session key yet :(
				throw new Exception("session receivekey is missing");
			}
			Debug.Log("Chain receive index is at least not null :)");

			ulong minIndex = 0;
			// check to NOT get an overflow (was a bug, ate about 20 hours)
			if (index > 40)
				minIndex = index - 30;
			if (minIndex > receiveKeyIndex)
			{
				Debug.Log("Chain receive index is to low it is:" + receiveKeyIndex + " and should be : " + minIndex);
				// the current key is to far behind, ratchet the key forward
				while (minIndex > receiveKeyIndex)
				{
					// this also increases the receiveKeyIndex
					RatchetReceiveKey();
				}
				//sessionReceiveKey = AdvanceKey(sessionReceiveKey, (int)(minIndex - receiveKeyIndex));
			}
			Debug.Log("Chain receive key is:" + JsonUtility.ToJson(AdvanceKey(sessionReceiveKey, (int)(index - receiveKeyIndex))));
			return ChainToEncryptionKey(AdvanceKey(sessionReceiveKey, (int)(index - receiveKeyIndex)));
		}


		private void RatchetReceiveKey()
		{
			sessionReceiveKey = RatchetKey(sessionReceiveKey);
			receiveKeyIndex++;
		}

		public static byte[] RatchetKey(byte[] key)
		{
			return Hash(ConcatBytes(key, new byte[] { 0x02 }));
		}

		public static byte[] GetEncKeyFromChain(byte[] chainKey)
		{
			return Hash(ConcatBytes(chainKey, new byte[] { 0x01 }));
		}


		public void SetKeys(byte[] publicIdentKey, byte[] publicPreKey, byte[] PublicOneTimeKey)
		{
			this.publicOneTimeKey = PublicOneTimeKey;
			this.publicIdentKey = publicIdentKey;
			this.publicPreKey = publicPreKey;
		}


		public void SetServerKeys(ChatSetupHeader header, KeyPair oneTimeKey)
		{
			this.publicEphemeralKey = header.publicEphemeralKey;
			this.publicPreKey = header.publicPreKey;
			// test the ident key to be the same;

			if (this.publicIdentKey != null && this.publicIdentKey.Length != 0 && !this.publicIdentKey.SequenceEqual(header.publicIdentKey))
			{
				Debug.Log(JsonUtility.ToJson(this) + "  " + JsonUtility.ToJson(header));

				throw new Exception("Attack on e2e:/wrong ident key");
			}
			this.publicIdentKey = header.publicIdentKey;
			this.oneTimeKeyPair = oneTimeKey;
			this.isServer = true;
		}


		public byte[] GetIdentKey()
		{
			return publicIdentKey;
		}


		public bool HasSessionKeys()
		{
			if (sessionSendKey != null && sessionReceiveKey != null)
				return true;
			return false;
		}

		public bool HasReceivedSetup()
		{
			return hasReceived;
		}

		public void ReceivedSetup()
		{
			hasReceived = true;
		}


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


		public bool GetIsServer()
		{
			return isServer;
		}


		public SourceReference GetIdentifier()
		{
			return this.identifier;
		}

		public ulong GetReceiveKeyIndex()
		{
			return receiveKeyIndex;
		}

		public ulong GetSendKeyIndex()
		{
			return sendKeyIndex;
		}

		public static byte[] AdvanceKey(byte[] key, int max)
		{
			byte[] tmpKey = key;
			for (int i = 0; i < max; i++)
			{
				tmpKey = RatchetKey(key);
			}
			return tmpKey;
		}


		public byte[] GetSessionSendKey()
		{
			return sessionSendKey;

		}
	}



	[System.Serializable]
	public class ChatSetupHeader
	{
		public byte[] publicIdentKey;
		public byte[] publicPreKey;
		public byte[] publicEphemeralKey;
		public byte[] publicOneTimeKey;
	}



	[System.Serializable]
	public class UserKeys
	{
		public KeyPair identKey;
		public KeyPair preKey;
		public KeyPair[] oneTimeKeys;

		public UserKeys()
		{
			oneTimeKeys = new KeyPair[64];
		}
	}
}