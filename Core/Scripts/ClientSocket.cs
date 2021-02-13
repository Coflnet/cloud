using System.Collections;
using System.Collections.Generic;
using Coflnet;
using Coflnet.Server;
using WebSocketSharp;
using MessagePack;

namespace Coflnet
{
	/// <summary>
	/// Client socket establishes a connection to some server.
	/// </summary>
	public class ClientSocket : ICommandTransmit
	{
		private WebSocket webSocket;

		private event ReceiveCommandData onMessage;
		public delegate void CoflnetExceptionEvent(CoflnetException coflnetException);
		public event CoflnetExceptionEvent OnError;

		public static ClientSocket Instance;

		private EntityId ConnectedServerId = ConfigController.ApplicationSettings.id;

		static ClientSocket()
		{
			Instance = NewInstance();
			Instance.webSocket.Log.Level = LogLevel.Error;
			Instance.webSocket.Log.Output += (arg1, arg2) =>
			{
				Logger.Log(arg1);
				Logger.Log(arg2);
			};

			Instance.webSocket.Connect();
		}


		public static ClientSocket NewInstance()
		{
			return new ClientSocket(new WebSocket(ConfigController.GetUrl("socket", ConfigController.WebProtocol.wss)));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Client.ClientSocket"/> class.
		/// </summary>
		/// <param name="socket">Websocket instance over which to comunicate.</param>
		public ClientSocket(WebSocket socket)
		{
			this.webSocket = socket;

			socket.OnOpen += (object sender, System.EventArgs e) => OnOpen(e);
			socket.OnError += (object sender, ErrorEventArgs e) =>
			{

			};
			socket.OnMessage += OnMessage;
		}

		public void OnMessage(object sender, MessageEventArgs e)
		{
			try
			{
				var data = MessagePackSerializer.Deserialize<CommandData>(e.RawData);
				if (data.Type == "error")
				{
					// errors contain aditional attributes 
					var error = MessagePackSerializer.Deserialize<CoflnetExceptionTransmit>(e.RawData);
					OnError.Invoke(new CoflnetException(error));
					return;
				}

				onMessage?.Invoke(data);

				// confirm receival
				SendCommand(
					CommandData.CreateCommandData<ReceiveConfirm, ReceiveConfirmParams>(
						ConnectedServerId,
						new ReceiveConfirmParams(data.SenderId, data.MessageId)));
			}
			catch (System.Exception ex)
			{
				Logger.Error(ex.Message);
			}


		}

		private void OnOpen(System.EventArgs e)
		{
			// authorize client if possible
		}

		/// <summary>
		/// Sends a command to the connected server.
		/// </summary>
		/// <param name="data">Data.</param>
		public bool SendCommand(CommandData data)
		{
			return SendCommand(data, true);
		}

		public bool SendCommand(CommandData data, bool changeSender)
		{
			if (changeSender)
				// add the userId if present as sender
				data.SenderId = ConfigController.ActiveUserId;

			if(!webSocket.IsConnected)
			{
				return false;
			}

			webSocket.Send(MessagePackSerializer.Serialize(data));
			return true;
		}

		/// <summary>
		/// Adds an on Message Callback
		/// </summary>
		/// <param name="callback">Callback.</param>
		public void AddCallback(ReceiveCommandData callback)
		{
			onMessage += callback;
		}

		/// <summary>
		/// Removes an on Message Callback
		/// </summary>
		/// <param name="callback">Callback.</param>
		public void RemoveCallback(ReceiveCommandData callback)
		{
			onMessage -= callback;
		}

		/// <summary>
		/// Invokes the on message event
		/// </summary>
		/// <param name="data">Data.</param>
		protected void InvokeOnMessage(CommandData data)
		{
			onMessage?.Invoke(data);
		}


		public void Reconnect()
		{
			webSocket.Connect();
		}

		/// <summary>
		/// Disconnect this instance.
		/// Closes the connection
		/// </summary>
		public void Disconnect()
		{
			webSocket.Close();
		}
	}

	/// <summary>
	/// Can transmit commands.
	/// </summary>
	public interface ICommandTransmit
	{
		bool SendCommand(CommandData data);

		/// <summary>
		/// Bevore executing the callback the implementation has to make sure that 
		/// the sender (<see cref="CommandData.SenderId"/>) is who he says
		/// </summary>
		/// <param name="callback"></param>
		void AddCallback(ReceiveCommandData callback);

	}

	public delegate void ReceiveCommandData(CommandData data);
}
