using System.Collections;
using System.Collections.Generic;
using MessagePack;
using System;

namespace Coflnet
{

	/// <summary>
	/// Abstraction of the filecontroller,
	/// Supports encryption and callback driven loading of data.
	/// Will encrypt and try to decrypt everything by default.
	/// </summary>
	public class DataController
	{
		public delegate void DataCallback(byte[] data);
		public delegate void DynamicDataCallback<T>(T data);

		public static bool encryptionLoaded { get; private set; }

		private Queue<KeyValuePair<string, DataCallback>> callbacks;

		protected byte[] encryptionKey = new byte[32];

		public static DataController Instance { get; }

		static DataController()
		{
			Instance = new DataController();
		}


		/// <summary>
		/// Loads an object asyncroniously.
		/// Handles Loading, decryption, deserialization and will invoke the given callback when done.
		/// </summary>
		/// <param name="path">Relative path to the data folder.</param>
		/// <param name="callback">Callback to execute when done.</param>
		/// <typeparam name="T">The type to serialize to.</typeparam>
		public void LoadObjectAsync<T>(string path, DynamicDataCallback<T> callback) where T : new()
		{
			LoadDataAsync(path, data => callback.Invoke(MessagePackSerializer.Deserialize<T>(data)));
		}

		/// <summary>
		/// Saves object after encryption.
		/// </summary>
		/// <param name="path">Relative path to the data folder.</param>
		/// <param name="objectToSave">Object to save.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public void SaveObject<T>(string path, T objectToSave)
		{
			SaveData(path, MessagePackSerializer.Serialize<T>(objectToSave));
		}


		/// <summary>
		/// Loads data asyncroniously.
		/// Handles Loading and decryption, will invoke the given callback when done
		/// </summary>
		/// <param name="path">Relative path to the data folder.</param>
		/// <param name="callback">Callback to execute when done.</param>
		public void LoadDataAsync(string path, DataCallback callback)
		{
			if (!encryptionLoaded)
				callbacks.Enqueue(new KeyValuePair<string, DataCallback>(path, callback));
			else
				LoadDataAsyncFromDisc(path, callback);
		}

		/// <summary>
		/// Saves the data after encryption
		/// </summary>
		/// <param name="path">Path relative to the data folder.</param>
		/// <param name="data">Data.</param>
		public void SaveData(string path, byte[] data)
		{
			FileController.WriteAllBytes(path, Encrypt(data));
		}

		/// <summary>
		/// Loads the data async from disc, decrypts it and executes the callback with it.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="callback">Callback to execute after load.</param>
		private void LoadDataAsyncFromDisc(string path, DataCallback callback)
		{
			callback(LoadData(path));
		}

		/// <summary>
		/// Loads data from disc and tries to decrypt it.
		/// </summary>
		/// <returns>The decrypted data.</returns>
		/// <param name="relativePath">Relative path.</param>
		public byte[] LoadData(string relativePath)
		{
			return Decrypt(FileController.ReadAllBytes(relativePath));
		}

		/// <summary>
		/// Loads data from disc and tries to decrypt and deserialize it.
		/// </summary>
		/// <returns>The loaded object.</returns>
		/// <param name="relativePath">Path relative to the data folder.</param>
		/// <typeparam name="T">Type to deserialize to.</typeparam>
		public T LoadObject<T>(string relativePath)
		{
			return MessagePackSerializer.Deserialize<T>(LoadData(relativePath));
		}

		/// <summary>
		/// Decrypt the specified data with the encryption key
		/// </summary>
		/// <returns>The decrypt.</returns>
		/// <param name="data">Data.</param>
		private byte[] Decrypt(byte[] data)
		{
			return EndToEndEncrypt.DecryptData(data, encryptionKey);
		}

		/// <summary>
		/// Encrypt the specified data with the encryption key
		/// </summary>
		/// <returns>The encrypted data.</returns>
		/// <param name="data">Data to encrypt.</param>
		private byte[] Encrypt(byte[] data)
		{
			return EndToEndEncrypt.EncryptData(data, encryptionKey);
		}

		/// <summary>
		/// Executes all callbacks when encryption is ready
		/// </summary>
		private void EncryptionReady()
		{
			while (callbacks.Count > 0)
			{
				var item = callbacks.Dequeue();
				LoadDataAsyncFromDisc(item.Key, item.Value);
			}
		}

		/// <summary>
		/// Sets the encryption key.
		/// </summary>
		/// <param name="key">Key.</param>
		public void SetEncryptionKey(byte[] key)
		{
			if (key.Length != 32)
				throw new Exception("the encryption key has to be 32 bytes");
			encryptionKey = key;
		}

		/// <summary>
		/// Derives the encryption key from a passphrase.
		/// </summary>
		/// <param name="phrase">Phrase.</param>
		public void DeriveEncryptionKeyFromPassphrase(string phrase)
		{
			SetEncryptionKey(CoflnetEncryption.Hash(System.Text.Encoding.UTF8.GetBytes(phrase)));
		}

		/// <summary>
		/// Removes objects of T that match func from file at path;
		/// </summary>
		/// <param name="path">File that should be searched.</param>
		/// <param name="func">Function to use for determing if object should be removed.</param>
		/// <typeparam name="T">What type of object are stored in this file (has to be the same for each object).</typeparam>
		public void RemoveFromFile<T>(string path, Func<T, bool> func)
		{
			if (FileController.Exists(path))
			{
				var tempName = path + ".tmp";

				lock (tempName)
				{
					bool any = false;
					foreach (var item in FileController.ReadLinesAs<T>(path))
					{
						if (func.Invoke(item))
						{
							FileController.AppendLineAs<T>(tempName, item);
							any = true;
						}
					}
					FileController.Delete(path);
					if (any)
						FileController.Move(tempName, path);
				}
			}
		}
	}
}


