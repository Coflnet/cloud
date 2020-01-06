using System;
using System.Collections;
using System.Collections.Generic;
using Coflnet;
using Coflnet.Server;

namespace Coflnet.Dev
{


	public class SimulationInstance 
	{
		public CoflnetCore core {set;get;}

		/// <summary>
		/// Determines if the core appears to be connected or not (simulated)
		/// </summary>
		/// <value></value>
		public bool IsConnected {set;get;}

		/// <summary>
		/// Will be invoked on new message, return value determines if messages will be forwarded as usual
		/// </summary>
		public Func<MessageData,bool> OnMessage;

		/// <summary>
		/// Will be invoked after the command was processed successfully
		/// </summary>
		public Action<MessageData> AfterMessage;

		public void ReceiveCommand(DevMessageData data, SourceReference sender = default(SourceReference))
		{
			if(!IsConnected)
			{
				// whoops we have no network/internet (simulated)
				return;
			}
			UnityEngine.Debug.Log($"Executing on {core.Id} ({core.GetType().Name})");
			data.CoreInstance = core;
			if(OnMessage == null || OnMessage.Invoke(data)){
				core.ReceiveCommand(data,sender);
				AfterMessage?.Invoke(data);
			}
		}

		public SimulationInstance()
		{
			IsConnected = true;
		}
	}
}
