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
		public Func<CommandData,bool> OnMessage;

		/// <summary>
		/// Will be invoked after the command was processed successfully
		/// </summary>
		public Action<CommandData> AfterMessage;

		public void ReceiveCommand(DevCommandData data, EntityId sender = default(EntityId))
		{
			if(!IsConnected)
			{
				// whoops we have no network/internet (simulated)
				return;
			}
						data.CoreInstance = core;
			// only execute if there is no onmessage or onmessage allows it
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
