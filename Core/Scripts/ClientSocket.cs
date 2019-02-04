﻿using System.Collections;
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

		private event ReceiveMessageData onMessage;
		public delegate void CoflnetExceptionEvent(CoflnetException coflnetException);
		public event CoflnetExceptionEvent OnError;

		public static ClientSocket Instance;

		static ClientSocket()
		{
			var url = ConfigController.GetUrl("socket", ConfigController.WebProtocol.wss);
			UnityEngine.Debug.Log("url ist: " + url);

			Instance = new ClientSocket(new WebSocket(url));
			Instance.webSocket.Log.Level = LogLevel.Trace;
			Instance.webSocket.Log.Output += (arg1, arg2) =>
			{
				UnityEngine.Debug.Log("clientsocket: " + arg1);
			};
			Instance.webSocket.Connect();
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

				UnityEngine.Debug.Log("socket error: " + e.Message);
			};
			socket.OnMessage += (object sender, MessageEventArgs e) =>
			{
				var data = MessagePackSerializer.Deserialize<MessageData>(e.RawData);
				if (data.t == "error")
				{
					// errors contain aditional attributes 
					var error = MessagePackSerializer.Deserialize<CoflnetExceptionTransmit>(e.RawData);
					OnError.Invoke(new CoflnetException(error));
					return;
				}

				onMessage?.Invoke(data);
				UnityEngine.Debug.Log("socket response: " + MessagePackSerializer.ToJson(e.RawData));
			};
		}

		private void OnOpen(System.EventArgs e)
		{
			// authorize client if possible
			UnityEngine.Debug.Log("opened socket");
		}

		/// <summary>
		/// Sends a command to the connected server.
		/// </summary>
		/// <param name="data">Data.</param>
		public void SendCommand(MessageData data)
		{
			webSocket.Send(MessagePackSerializer.Serialize(data));
		}

		/// <summary>
		/// Adds an on Message Callback
		/// </summary>
		/// <param name="callback">Callback.</param>
		public void AddCallback(ReceiveMessageData callback)
		{
			onMessage += callback;
		}

		/// <summary>
		/// Removes an on Message Callback
		/// </summary>
		/// <param name="callback">Callback.</param>
		public void RemoveCallback(ReceiveMessageData callback)
		{
			onMessage -= callback;
		}

		/// <summary>
		/// Invokes the on message event
		/// </summary>
		/// <param name="data">Data.</param>
		protected void InvokeOnMessage(MessageData data)
		{
			onMessage?.Invoke(data);
		}
	}

	/// <summary>
	/// Can transmit a command.
	/// </summary>
	public interface ICommandTransmit
	{
		void SendCommand(MessageData data);

		void AddCallback(ReceiveMessageData callback);

	}

	public delegate void ReceiveMessageData(MessageData data);
}
