using System;
using MessagePack;

namespace Coflnet.Server
{
	public class LoginServer : Command
	{
		public override void Execute(MessageData data)
		{
			var loginParams = data.GetAs<ServerLoginToken>();
			if (loginParams == null)
			{
				return;
			}
			var serverMessageData = data as ServerMessageData;

			var server = ReferenceManager.Instance.GetResource<CoflnetServer>(loginParams.OriginServerId);
			if (loginParams.Validate(server.PublicKey, ConfigController.ApplicationSettings.id))
			{
				// validation success
				serverMessageData.Connection.AuthenticatedIds.Add(loginParams.OriginServerId);
				//data.SendBack()
			}
		}

		public override CommandSettings GetSettings()
		{
			return new CommandSettings();
		}

		public override string GetSlug()
		{
			return "loginServer";
		}
	}


	[MessagePackObject]
	public class ServerLoginToken
	{
		/// <summary>
		/// The origin server identifier trying to authenticate
		/// </summary>
		[Key(0)]
		public SourceReference OriginServerId;
		[Key(1)]
		public byte[] signature;
		[Key(2)]
		public DateTime time;
		/// <summary>
		/// Your server identifier.
		/// Or the target server Identifier
		/// </summary>
		[Key(3)]
		public SourceReference targetServerId;

		/// <summary>
		/// Validate the Token.
		/// </summary>
		/// <returns>The validate.</returns>
		/// <param name="publicKey">Public key.</param>
		public bool Validate(byte[] publicKey, SourceReference currentServer)
		{
			var TimeAmount = new TimeSpan(1, 0, 0);
			if (time < DateTime.Now.Subtract(TimeAmount))
			{
				throw new LoginFailedException($"The signed DateTime timed out, it is older than {TimeAmount.TotalMinutes} miniutes");
			}


			if (currentServer != targetServerId)
			{
				throw new LoginFailedException("The token wasn't made for this server");
			}


			return (CoflnetEncryption.SignVertifyDetached(signature, TokenContent, publicKey));
		}

		public void GenerateAndSetSignature(KeyPair keys)
		{
			if (keys == null || keys.secretKey == null)
			{
				throw new NullReferenceException($"{nameof(keys)} has to be set");
			}
			signature = CoflnetEncryption.SignByteDetached(TokenContent, keys);
		}


		private byte[] TokenContent
		{
			get
			{
				return IEncryption.ConcatBytes(BitConverter.GetBytes(time.Ticks), targetServerId.AsByte);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.ServerLoginToken"/> class.
		/// Also generates and sets signature
		/// </summary>
		/// <param name="OriginServerId">Server identifier.</param>
		/// <param name="time">Time.</param>
		/// <param name="keyPair">Key pair.</param>
		public ServerLoginToken(SourceReference OriginServerId, DateTime time, KeyPair keyPair, SourceReference targetServerId)
		{
			this.OriginServerId = OriginServerId;
			this.time = time;
			this.targetServerId = targetServerId;
			GenerateAndSetSignature(keyPair);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.ServerLoginToken"/> class.
		/// Also generates and sets signature
		/// </summary>
		/// <param name="serverId">Server identifier.</param>
		/// <param name="time">Time.</param>
		/// <param name="keyPair">Key pair.</param>
		public ServerLoginToken(SourceReference serverId, DateTime time, KeyPair keyPair) : this(serverId, time, keyPair, ConfigController.ApplicationSettings.id)
		{
		}


		public ServerLoginToken()
		{
		}

	}
	public class LoginFailedException : CoflnetException
	{
		public LoginFailedException(string message) : base("login_failed", message, "try again")
		{
		}
	}
}