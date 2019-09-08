using System;
using System.Collections.Generic;
using System.Text;
using Coflnet.Core.Crypto;
using MessagePack;
using Coflnet.Extentions;

namespace Coflnet
{
	/// <summary>
	/// Manages signed authentication Tokens
	/// </summary>
	public class TokenManager
	{
		public static TokenManager Instance;

		private Dictionary<byte[],byte[]> tokens;

		/// <summary>
		/// Adds a new token
		/// </summary>
		/// <param name="identifier">The identifier for this token. May be the domain or similar.</param>
		/// <param name="token">The actual token to store</param>
		public void AddToken(string identifier,string token)
		{
			tokens.Add(Encoding.UTF8.GetBytes(identifier),Encoding.UTF8.GetBytes(token));
		}

		/// <summary>
		/// Adds a new token
		/// </summary>
		/// <param name="target">The target <see cref="Referenceable"/> this token can be used for</param>
		/// <param name="token">The actual token value</param>
		public void AddToken(SourceReference target, Token token,SourceReference sender = default(SourceReference))
		{
			AddToken(target,MessagePackSerializer.Serialize(token));
		}

		/// <summary>
		/// Adds a new token
		/// </summary>
		/// <param name="target"></param>
		/// <param name="serializedToken"></param>
		public void AddToken(SourceReference target, byte[] serializedToken,SourceReference sender = default(SourceReference))
		{
			if(sender == default(SourceReference))
				tokens.Add(target.AsByte,serializedToken);
			else
			{
				tokens.Add(target.AsByte.Append(sender.AsByte),serializedToken);
			}
		}


		/// <summary>
		/// Gets an external token found by its identifier
		/// </summary>
		/// <param name="identifier">The identifier to search for</param>
		/// <returns>The token</returns>
		public string GetExternalToken(string identifier)
		{
			return Encoding.UTF8.GetString(tokens[Encoding.UTF8.GetBytes(identifier)]);
		}


		/// <summary>
		/// Gets a Token
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public Token GetToken(SourceReference target,SourceReference sender = default(SourceReference))
		{
			return MessagePackSerializer.Deserialize<Token>(GetInternalToken(target));
		}

		public byte[] GetInternalToken(SourceReference target,SourceReference sender = default(SourceReference))
		{
			if(sender == default(SourceReference))
				return tokens[target.AsByte];
			return tokens[target.AsByte.Append(sender.AsByte)];
		}

		static TokenManager()
		{
			Instance = new TokenManager();
			DataController.Instance.RegisterSaveCallback(Instance.Save);
		}

		public TokenManager()
		{
			DataController.Instance.RegisterSaveCallback(Save);
		}

		/// <summary>
		/// Generates a new instance of the <see cref="TokenManager"/> 
		/// and loads tokens found in the given <see cref="DataController"/>
		/// </summary>
		/// <param name="dc">the <see cref="DataController"/> to store and load tokens from</param>
		public TokenManager(DataController dc)
		{
			dc.RegisterSaveCallback(Save);
			LoadTokens(dc);
		}

		/// <summary>
		/// Loads tokens from a given <see cref="DataController"/>.
		/// Will overwrite any loaded tokens
		/// </summary>
		/// <param name="dc">The <see cref="DataController"/> to load from</param>
		public void LoadTokens(DataController dc)
		{
			tokens = dc.LoadObject<Dictionary<byte[],byte[]>>(
                "tokens",
                () => new Dictionary<byte[], byte[]>());
		}

		/// <summary>
		/// Saves all loaded tokens
		/// </summary>
		/// <param name="dc">The <see cref="DataController"/> to save to</param>
		public void Save(DataController dc)
		{
			dc.SaveObject("tokens",tokens);
		}

		/// <summary>
		/// Generates a new Token for some <see cref="subject"/> 
		/// </summary>
		/// <param name="subject">The subject to generate the token for</param>
		/// <param name="issuer">As who to create the token</param>
		/// <param name="signingPair">The <see cref="SigningKeyPair"/> of the <see cref="issuer"/></param>
		/// <param name="claims">Optional additional claims to add to the token</param>
		/// <returns>The newly created and signed token</returns>
		public Token GenerateNewToken(
            SourceReference subject,
            SourceReference issuer,
            SigningKeyPair signingPair,
            params KeyValuePair<string, string>[] claims)
		{
			return new Token()
				.AddClaim("iss",issuer.ToString())
				.AddClaim("sub",subject.ToString())
				.AddClaim("iat",DateTime.UtcNow.ToFileTimeUtc().ToString())
				.AddClaims(claims)
				.Sign(signingPair);
		}
	}

	[MessagePackObject]
	public class Token
	{
		[Key(0)]
		public Dictionary<string,string> claims;
		[Key(1)]
		public Signature signature;

        public Token(Dictionary<string, string> claims, Signature algorythm)
        {
            this.claims = claims;
            this.signature = algorythm;
        }


		public Token() : this(new Dictionary<string, string>(),new Signature())
        {
        }




        /// <summary>
        /// Signs the Token with a KeyPair
        /// </summary>
        /// <param name="keyPair">The keypair to use to sign the token</param>
        public Token Sign(SigningKeyPair keyPair)
		{
			signature.algorythm = keyPair.algorythm;
			signature.GenerateSignature(SignableContent,keyPair);
			return this;
		}

		/// <summary>
		/// Validates the signature on the token
		/// </summary>
		/// <param name="publicKey">The Public part of the secret key if asymetric algrorythm or the secret if a symetric algorythm was used</param>
		/// <returns><see cref="true"/> if the token is valid</returns>
		public bool Validate(byte[] publicKey)
		{
			return signature.ValidateSignature(SignableContent,publicKey);
		}

		/// <summary>
		/// Adds a claim to the token
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public Token AddClaim(string key, string value)
		{
			this.claims.Add(key,value);
			return this;
		}

		/// <summary>
		/// Adds multiple claims at once 
		/// </summary>
		/// <param name="claims">Claims to add</param>
		/// <returns>The same object the function was executed on</returns>
		public Token AddClaims(IEnumerable<KeyValuePair<string,string>> claims)
		{
			foreach (var item in claims)
			{
				this.claims.Add(item.Key,item.Value);
			}
			return this;
		}

		private byte[] SignableContent => MessagePackSerializer.Serialize(claims);
	}
}