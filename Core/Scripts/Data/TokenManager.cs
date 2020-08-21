using System;
using System.Collections.Generic;
using System.Text;
using Coflnet.Core.Crypto;
using MessagePack;
using Coflnet.Extentions;
using System.Linq;

namespace Coflnet
{
	/// <summary>
	/// Manages signed authentication Tokens
	/// </summary>
	public class TokenManager
	{
		public static TokenManager Instance;

		private Dictionary<byte[],byte[]> tokens;

		private HashSet<MessageReference> invokedTokens;

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
		/// <param name="target">The target <see cref="Entity"/> this token can be used for</param>
		/// <param name="token">The actual token value</param>
		/// <param name="sender"></param>
		public void AddToken(EntityId target, Token token,EntityId sender = default(EntityId))
		{
			AddToken(target,MessagePackSerializer.Serialize(token),sender);
		}

		/// <summary>
		/// Adds a new token
		/// </summary>
		/// <param name="target"></param>
		/// <param name="serializedToken"></param>
		/// <param name="sender"></param>
		public void AddToken(EntityId target, byte[] serializedToken,EntityId sender = default(EntityId))
		{
			if(sender == default(EntityId))
				tokens.Add(target.AsByte,serializedToken);
			else
			{
				tokens.Add(target.AsByte.Append(sender.AsByte),serializedToken);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public bool IsTokenInvoked(Token token)
		{
			return invokedTokens.Contains(token.Id);
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
		public Token GetToken(EntityId target,EntityId sender = default(EntityId))
		{
			return MessagePackSerializer.Deserialize<Token>(GetInternalToken(target));
		}

		/// <summary>
		/// Tries to get the token
		/// </summary>
		/// <param name="target">The target <see cref="Entity"/> this token is made for</param>
		/// <param name="token">The variable to pass the token to</param>
		/// <param name="sender">Optional sender the token was made for</param>
		/// <returns><see cref="true"/> if the token was found <see cref="false"/> otherwise</returns>
		public bool TryGetToken(EntityId target,out Token token, EntityId sender= default(EntityId))
		{
			var key = target.AsByte;
			if(sender != default(EntityId))
			{
				key = key.Append(sender.AsByte);
			}

			byte[] serializedToken;
			if(tokens.TryGetValue(key,out serializedToken))
			{
				token = MessagePackSerializer.Deserialize<Token>(serializedToken);
				return true;
			}
			token = null;
			return false;
		}

		public byte[] GetInternalToken(EntityId target,EntityId sender = default(EntityId))
		{
			if(sender == default(EntityId))
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
		/// <param name="claims">Optional additional claims to add to the token, may not be one of [iss,sub,id,iat] as these are already set</param>
		/// <returns>The newly created and signed token</returns>
		public Token GenerateNewToken(
            EntityId subject,
            EntityId issuer,
            SigningKeyPair signingPair,
            params KeyValuePair<string, string>[] claims)
		{
			return new Token()
				.AddClaim("iss",issuer.ToString())
				.AddClaim("sub",subject.ToString())
				.AddClaim("id",ThreadSaveIdGenerator.NextId.ToString())
				.AddClaim("iat",DateTime.UtcNow.ToFileTimeUtc().ToString())
				.AddClaims(claims)
				.Sign(signingPair);
		}

		/// <summary>
		/// Generates a new Token for some <see cref="subject"/> 
		/// </summary>
		/// <param name="subject">The subject to generate the token for</param>
		/// <param name="issuer">As who to create the token (the owner of the <see cref="signingPair"/>)</param>
		/// <param name="signingPair">The <see cref="SigningKeyPair"/> of the <see cref="issuer"/></param>
		/// <param name="expirationTime">The unix time when this token expires</param>
		/// <param name="claims">Optional additional claims to add to the token, may not be one of [iss,sub,id,iat] as these are already set</param>
		/// <returns>The newly created and signed token</returns>
		public Token GenerateNewToken(
            EntityId subject,
            EntityId issuer,
            SigningKeyPair signingPair,
			long expirationTime,
            params KeyValuePair<string, string>[] claims)
		{
			return ConstructToken(subject,issuer,expirationTime)
				.AddClaims(claims)
				.Sign(signingPair);
		}

		private Token ConstructToken(
			EntityId subject,
            EntityId issuer,
			long expirationTime)
			{
				return new Token()
				.AddClaim("iss",issuer.ToString())
				.AddClaim("sub",subject.ToString())
				.AddClaim("id",ThreadSaveIdGenerator.NextId.ToString())
				.AddClaim("exp",expirationTime.ToString());
			}


		/// <summary>
		/// Finds out if a given token is valid for a specific target.
		/// </summary>
		/// <param name="token">The token to test</param>
		/// <param name="target">The target tried to be accessed</param>
		/// <param name="sender">The sender that is trying to access the target</param>
		/// <returns><see cref="true"/> if token is valid for all given options and not invoked</returns>
		public bool IsTokenValid(Token token,Entity target,EntityId sender = default(EntityId))
		{
			// get the issuers key
			var key = KeyPairManager.Instance.GetSigningPublicKey(token.Issuer,token.signature.algorythm);

			// make sure the signature is valid
			if(!token.Validate(key.publicKey))
				return false;

			// validate that the token is for the target
			if(token.HasClaim("sub"))
			{
				var sub = token.GetClaimSR("sub");
				if(sub != target.Id)
				{
					throw new CoflnetException(
						"token_invalid",
						$"The target {nameof(Entity)} id ({target.Id}) is not the one the token provides ({sub})");
				}
			}

			if(sender != default(EntityId))
			{
				// validate the sender
				var intendedAudience =  token.GetClaimSR("aud");
				if(sender != intendedAudience)
				{
					throw new CoflnetException(
						"token_invalid",
						$"The sender ({sender}) is not the one the token grants access ({intendedAudience})");
				}
			}
			

			// make sure the issuer has basic access to the resource
			var issuer = token.GetClaimSR("iss");
			if(!target.IsAllowedAccess(issuer))
			{
				throw new CoflnetException(
                    "token_invalid",
                    $"The issuer of the token ({issuer}) has no access to ({target.Id})");
			}

			// make sure the token expiration time is in the future
			if(token.HasClaim("exp"))
			{
				if(token.GetClaimInt("exp") < CurrentUnixTime)
				{
					throw new CoflnetException(
                    "token_invalid",
                    $"The token has expired");
				}
			}

			if(IsTokenInvoked(token))
				return false;


			return true;
		}

		private long CurrentUnixTime
		{
			get
			{
				return DateTime.UtcNow.ToFileTimeUtc();
			}
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

		[IgnoreMember]
		public EntityId Issuer
		{
			get
			{
				return GetClaimSR("iss");
			}
			set
			{
				AddClaim("iss",value.ToString());
			}
		}

		public string[] Scopes
		{
			get
			{
				return claims["scopes"].Split(',');
			}
			set
			{
				AddClaim("scooes",string.Join(",",value));
			}
		}

		public MessageReference Id
		{
			get
			{
				return new MessageReference(Issuer,GetClaimInt("id"));
			}
			set
			{
				AddClaim("id",value.IdfromSource.ToString());
			}
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
		/// Wherether nor not the token has a specific claim
		/// </summary>
		/// <param name="key">The key to the claim</param>
		/// <returns></returns>
		public bool HasClaim(string key)
		{
			return claims.ContainsKey(key);
		} 

		/// <summary>
		/// Tries to get a specific claim as Int
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public long GetClaimInt(string key)
		{
			long result;
			if(!long.TryParse(claims[key],out result))
			{
				throw new CoflnetException("invalid_token",$"The {key} claim of the token is no int");
			}
			return result;
		}

		/// <summary>
		/// Tries to get a specific claim as <see cref="EntityId"/>
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public EntityId GetClaimSR(string key)
		{
			EntityId result;
			if(!EntityId.TryParse(claims[key],out result))
			{
				throw new CoflnetException("invalid_token",$"The {key} claim of the token is not a {nameof(EntityId)}");
			}
			return result;
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