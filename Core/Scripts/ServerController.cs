using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System;

namespace Coflnet
{
	public class ServerController
	{
		private ConcurrentDictionary<long, CoflnetServer> _servers;


		protected ConcurrentDictionary<long, CoflnetServer> Servers
		{
			get
			{
				if (_servers == null)
				{
					_servers = new ConcurrentDictionary<long, CoflnetServer>();
				}
				return _servers;
			}
			private set
			{
				_servers = value;
			}
		}


		public static ServerController Instance { get; set; }




		public void SendCommandToServer(MessageData data, long serverId = 0)
		{
			if (serverId == 0 || !Servers.ContainsKey(serverId))
			{
				throw new System.Exception("There is no server with the id " + serverId.ToString("X"));
			}
			if (serverId == 0)
				serverId = ConfigController.PrimaryServer;

			SendCommandToServer(data, Servers[serverId]);
		}

		/// <summary>
		/// Sends a command to a server.
		/// </summary>
		/// <param name="command">The command to execute on the other server.</param>
		/// <param name="data">Optional data to include.</param>
		/// <param name="serverId">Server identifier of the target server.</param>
		/// <typeparam name="T">What object type the data has .</typeparam>
		public void SendCommandToServer<T>(Command command, T data, long serverId)
		{

			MessageData message = new MessageData(command.GetSlug(),
												 MessagePack.MessagePackSerializer.Serialize<T>(data));
			SendCommandToServer(message, serverId);
		}

		public void SendCommand<T, Y>(SourceReference to, Y data) where T : Command
		{
			var serialized = MessagePack.MessagePackSerializer.Serialize(data);


			SendCommand<T>(to, serialized);
		}

		public void SendCommand<C>(SourceReference to, byte[] data) where C : Command
		{
			var commandInstance = ((C)Activator.CreateInstance(typeof(C)));

			byte[] bytes;
			if (commandInstance.Settings.Encrypted)
			{
				bytes = EncryptionController.instance.EncryptForResource(to, data);
			}
			else
			{
				bytes = data;
			}
			var message = new MessageData(to, bytes, commandInstance.GetSlug());

			SendCommandToServer(message);
		}


		/// <summary>
		/// Sends a command to a server
		/// </summary>
		/// <param name="data">Data.</param>
		/// <param name="server">Server.</param>
		public void SendCommandToServer(MessageData data, CoflnetServer server)
		{
			var connection = server.Connection;
			connection.SendCommand(data);
		}

		public ServerController()
		{

		}

		static ServerController()
		{
			Instance = new ServerController();
		}


		/// <summary>
		/// Gets or creates a new Instance of a server Object.
		/// </summary>
		/// <returns>The or instantiate.</returns>
		/// <param name="serverId">Server identifier.</param>
		public CoflnetServer GetOrCreate(long serverId)
		{
			CoflnetServer result;
			Servers.TryGetValue(serverId, out result);
			if (result == null)
			{
				result = new CoflnetServer(serverId);
				Servers.TryAdd(serverId, result);
			}

			return result;
		}
	}


}
