using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MessagePack;
using Coflnet.Core.Crypto;

namespace Coflnet
{
	/// <summary>
	/// Manages KeyPairs
	/// </summary>
	public class KeyPairManager
	{
		private Dictionary<byte[], KeyWithTime> keys;
		/// <summary>
		/// (Signing) KeyPairs of different SourceReferences we have and can use
		/// </summary>
		private Dictionary<SourceReference,SigningKeyPair> signingKeyPairs;

		public static KeyPairManager Instance;

		static KeyPairManager()
		{
			Instance = new KeyPairManager();
			Instance.keys = DataController.Instance.LoadObject<Dictionary<byte[], KeyWithTime>>("coflnet_keyPairs");
			Instance.signingKeyPairs = DataController.Instance.LoadObject<Dictionary<SourceReference,SigningKeyPair>>("signingKeys");
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

		/// <summary>
		/// Adds a new Signing KeyPair and persists it
		/// </summary>
		/// <param name="owner">The owner of the keyPair</param>
		/// <param name="keyPair"></param>
		public void AddSigningKeyPair(SourceReference owner,SigningKeyPair keyPair)
		{
			signingKeyPairs.Add(owner,keyPair);
		}

		/// <summary>
		/// Gets or creates a new <see cref="SigningKeyPair"/> for some <see cref="SourceReference"/>.
		/// Uses the <see cref="EncryptionController.Instance.SigningAlgorythm"/> to create a new <see cref="SigningKeyPair"/> 
		/// if there is none yet.
		/// </summary>
		/// <param name="owner"></param>
		/// <returns>The existing or new <see cref="SigningKeyPair"/></returns>
		public SigningKeyPair GetOrCreateSigningPair(SourceReference owner)
		{
			if(!signingKeyPairs.ContainsKey(owner))
			{
				signingKeyPairs.Add(owner,EncryptionController.Instance.SigningAlgorythm.GenerateKeyPair()); 
			}
			return signingKeyPairs[owner];
		}

		/// <summary>
		/// Gets the <see cref="SigningKeyPair"/> for some <see cref="SourceReference"/>
		/// </summary>
		/// <param name="owner">The <see cref="SourceReference"/> to get the <see cref="SigningKeyPair"/> for</param>
		/// <returns>The <see cref="SigningKeyPair"/> for the <see cref="owner"/></returns>
		public SigningKeyPair GetSigningKeyPair(SourceReference owner)
		{
			if(!signingKeyPairs.ContainsKey(owner))
			{
				throw new KeyNotFoundException($"No keyPair was found for `{owner}`");
			}
			return signingKeyPairs[owner];
		}

		/// <summary>
		/// Removes the <see cref="SigningKeyPair"/> if it existed
		/// </summary>
		/// <param name="owner">The <see cref="SourceReference"/> to remove the <see cref="SigningKeyPair"/> for</param>
		/// <returns><see cref="true"/> if removing was successful</returns>
		public bool RemoveSigningKeyPair(SourceReference owner)
		{
			return signingKeyPairs.Remove(owner);
		}

		public void Save()
		{
			FileController.SaveAs("coflnet_keyPairs", keys);
			FileController.SaveAs("signingKeys", signingKeyPairs);
		}

		/// <summary>
		/// Finds the public Key for a resource and returns it if found
		/// </summary>
		/// <param name="issuer">The owner of the KeyPair</param>
		/// <param name="algorythm">The Algorythm to search for. Each algorythm may need different keys</param>
		/// <returns></returns>
        public SigningKeyPair GetSigningPublicKey(SourceReference issuer,SigningAlgorythm algorythm = null)
        {
            return GetSigningKeyPair(issuer);
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


	/// <summary>
	/// A <see cref="KeyPair"/> knowing which what algorythm it was created
	/// </summary>
	[MessagePackObject]
	public class SigningKeyPair : KeyPair
	{
		[Key(3)]
		public SigningAlgorythm algorythm;

		public SigningKeyPair()
        {
        }

        public SigningKeyPair(SigningAlgorythm algorythm)
        {
            this.algorythm = algorythm;
        }

        public SigningKeyPair(byte[] publicKey, byte[] privateKey) : base(publicKey, privateKey)
        {
        }

        public override bool Equals(object obj)
        {
            var pair = obj as SigningKeyPair;
            return pair != null &&
                   base.Equals(obj) &&
                   EqualityComparer<SigningAlgorythm>.Default.Equals(algorythm, pair.algorythm);
        }

        public override int GetHashCode()
        {
            var hashCode = 767081072;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<SigningAlgorythm>.Default.GetHashCode(algorythm);
            return hashCode;
        }
    }


	[System.Serializable]
	[MessagePackObject]
	public class KeyPair
	{
		[Key(0)]
		public byte[] publicKey;
		[Key(1)]
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

        public override bool Equals(object obj)
        {
            var pair = obj as KeyPair;
            return pair != null &&
                   EqualityComparer<byte[]>.Default.Equals(publicKey, pair.publicKey) &&
                   EqualityComparer<byte[]>.Default.Equals(secretKey, pair.secretKey);
        }

        public override int GetHashCode()
        {
            var hashCode = 869923855;
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(publicKey);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(secretKey);
            return hashCode;
        }
    }
}
