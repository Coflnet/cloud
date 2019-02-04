using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MessagePack;

namespace Coflnet
{
	/// <summary>
	/// Manages KeyPairs
	/// </summary>
	public class KeyPairManager
	{

		private Dictionary<byte[], KeyWithTime> keys;

		public static KeyPairManager Instance;

		static KeyPairManager()
		{
			Instance = new KeyPairManager();
			Instance.keys = FileController.LoadAs<Dictionary<byte[], KeyWithTime>>("coflnet_keyPairs");
		}

		/// <summary>
		/// Gets a key pair.
		/// </summary>
		/// <returns>The key pair, or null if the private part wasn't found.</returns>
		/// <param name="publicPart">Public part.</param>
		public KeyPair GetKeyPair(byte[] publicPart)
		{
			if (!keys.ContainsKey(publicPart))
				return null;
			return new KeyPair(publicPart, keys[publicPart].PrivateKey);
		}

		public KeyPairManager()
		{
			keys = new Dictionary<byte[], KeyWithTime>();
		}

		public void AddKey(KeyPair pair)
		{
			keys.Add(pair.publicKey, new KeyWithTime(pair.secretKey));
			Save();
		}

		public void Save()
		{
			FileController.SaveAs("coflnet_keyPairs", keys);
		}
	}

	[MessagePackObject]
	public class KeyWithTime
	{
		[Key(0)]
		private byte[] privateKey;
		[Key(1)]
		private DateTime dateTime;

		public byte[] PrivateKey
		{
			get
			{
				return privateKey;
			}
		}

		public DateTime DateTime
		{
			get
			{
				return dateTime;
			}
		}

		public KeyWithTime(byte[] privateKey, DateTime dateTime)
		{
			this.privateKey = privateKey;
			this.dateTime = dateTime;
		}

		public KeyWithTime(byte[] privateKey)
		{
			this.privateKey = privateKey;
			this.dateTime = DateTime.Now;
		}
	}


	[System.Serializable]
	public class KeyPair
	{
		public byte[] publicKey;
		public byte[] secretKey;

		public KeyPair(int publicKeyLength = 32, int privateKeyLength = 32)
		{
			publicKey = new byte[publicKeyLength];
			secretKey = new byte[privateKeyLength];
		}

		public KeyPair(byte[] publicKey, byte[] privateKey)
		{
			this.publicKey = publicKey;
			this.secretKey = privateKey;
		}
	}
}
