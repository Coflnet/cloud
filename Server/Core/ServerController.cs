﻿using System.Collections.Generic;
using System.IO;
using Coflnet;
using System.Text;
using WebSocketSharp;


namespace Coflnet.Server
{

	public class ServerController : Coflnet.ServerController
	{
		public CoflnetServer CurrentServer { get; protected set; }
		/// <summary> 
		/// this servers public and private key
		/// </summary>
		protected KeyPair serverKeys;
		/// <summary>
		/// This servers public key with its signature from the master server
		/// </summary>
		protected byte[] puglicKeyWithSignature;
		protected static Dictionary<string, byte[]> masterKeys;

		public static ServerController ServerInstance;

		static ServerController()
		{
			ServerInstance = new ServerController();
			Instance = ServerInstance;
		}



		/// <summary>
		/// Sends a command to a server, adds command to the command queue if the id is the current server.
		/// </summary>
		/// <param name="command">The command to execute on the other server.</param>
		/// <param name="data">Optional data to include.</param>
		/// <param name="serverId">Server identifier of the target server.</param>
		/// <typeparam name="T">What object type the data has .</typeparam>
		public new void SendCommandToServer<T>(Command command, T data, long serverId)
		{
			// get arround the serialization if the local server is the target
			if (serverId == CurrentServer.ServerId)
			{
				CoflnetCore.Instance.GetCommandController().ExecuteCommand(command, new CommandData()
				{
					DeSerialized = data
				});
				return;
			}

			CommandData message = new CommandData()
			{
				message = MessagePack.MessagePackSerializer.Serialize<T>(data),
				Type = command.Slug
			};
			SendCommandToServer(message, serverId);
		}

		/// <summary>
		/// Sends a command to a server
		/// </summary>
		/// <param name="data">Data.</param>
		/// <param name="server">Server.</param>
		public new void SendCommandToServer(CommandData data, CoflnetServer server)
		{
			// if it is the current server add it to the stack
			if (CurrentServer.PublicId == server.PublicId)
			{
				CoflnetCore.Instance.GetCommandController().ExecuteCommand(data);

				return;
			}

			var connection = server.Connection;
			connection.SendCommand(data);
		}


		public KeyPair ServerKeys
		{
			get
			{
				return serverKeys;
			}
		}

		public byte[] PuglicKeyWithSignature
		{
			get
			{
				return puglicKeyWithSignature;
			}
		}

		public byte[] GetAuthorityPublicKey(string identifier)
		{
			if (!masterKeys.ContainsKey(identifier))
			{
				throw new CoflnetException("unknown_ca", "unknown ca / master key");
			}
			return masterKeys[identifier];
		}


		public CoflnetServer FindServer(long identifier)
		{
			if (!Servers.ContainsKey(identifier))
			{
				Servers.TryAdd(identifier, new CoflnetServer(identifier));
			}
			return Servers[identifier];
		}
	}

	public interface IServerToServer
	{
		/// <summary>
		/// Sends some data
		/// </summary>
		/// <param name="data">Data.</param>
		void SendData(byte[] data);
	}

	/// <summary>
	/// Represents a connection to another server
	/// </summary>
	public abstract class ServerToServerConnection : IServerToServer
	{
		public delegate void DataReceiver(byte[] data);
		public event DataReceiver onDataRetrival;
		protected bool authenticated;
		protected CoflnetServer server;
		protected IEncryption encrypt;

		public void SendData(byte[] data)
		{
			SendEncryptedData(data);
		}

		protected void SendEncryptedData(byte[] data)
		{
			SendInternal(Encrypt(data));
		}

		public void ReceiveData(byte[] data)
		{
			if (onDataRetrival != null)
				onDataRetrival.Invoke(data);
		}

		public byte[] Encrypt(byte[] data)
		{
			//SslStream ssl = new SslStream(new MemoryStream());
			//ssl.AuthenticateAsClient("coflnet.com",null,System.Security.Authentication.SslProtocols.Tls12,)
			//ssl.KeyExchangeAlgorithm
			//   System.Security.Authentication.ExchangeAlgorithmType.RsaSign
			//ssl.ci

			// Get sure the session has been established
			if (!encrypt.HasEncryptionKeys())
			{
				//encrypt.Get
				//SendInternal<SecureConnectionServerSetupMessage>(encrypt.GetSetupDataServer());
			}

			return encrypt.EncryptWithSessionKey(data);
		}

		public byte[] Decrypt(byte[] data)
		{
			return encrypt.DecryptWithSessionKey(data);
		}

		protected void SendInternal<T>(T data)
		{
			SendInternal(MessagePack.MessagePackSerializer.Serialize<T>(data));
		}

		protected abstract void SendInternal(byte[] data);
	}


	/// <summary>
	/// Implements server to server connection over a websocket client
	/// </summary>
	public class WebsocketClientServerToServerConnection : ServerToServerConnection
	{
		protected WebSocket client;

		public WebsocketClientServerToServerConnection(WebSocket client)
		{
			this.client = client;
			client.OnMessage += (object sender, MessageEventArgs e) =>
			{
				ReceiveData(e.RawData);
			};
		}

		protected override void SendInternal(byte[] data)
		{
			client.SendAsync(data, null);
		}
	}


	/// <summary>
	/// Implements server to server connection over a websocket client
	/// </summary>
	public class WebsocketServerServerToServerConnection : ServerToServerConnection
	{
		protected CoflnetWebsocketServer websocketServer;

		public WebsocketServerServerToServerConnection(CoflnetWebsocketServer server)
		{
			this.websocketServer = server;
			// add the event listener
			// todo
		}

		protected override void SendInternal(byte[] data)
		{
			throw new System.Exception("not implemented");
			//CoflnetSocket.socketServer.SendToServer(data, this.server.PublicId);
		}
	}

	public class HttpServerToServerConnection : ServerToServerConnection
	{
		protected override void SendInternal(byte[] data)
		{
			throw new System.NotImplementedException();
		}
	}

}
