using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coflnet
{
	public class ReturnCommandService
	{
		public static ReturnCommandService Instance;

		protected Dictionary<long, Command.CommandMethod> callbacks = new Dictionary<long,Command.CommandMethod>();

		static ReturnCommandService()
		{
			Instance = new ReturnCommandService();
		}

		public void AddCallback(long id, Command.CommandMethod callback)
		{
			callbacks.Add(id, callback);
		}

		/// <summary>
		/// Receives a response message.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="data">Data.</param>
		public void ReceiveMessage(long id, CommandData data)
		{
			Command.CommandMethod command;
			if (callbacks.TryGetValue(id, out command))
			{
				command.Invoke(data);
			}
			else
			{
				// log that there wasn't a command found
				Logger.Error($"received response command {data.Type} from {data.SenderId} but didn't have a callback");
			}
		}
	}
}
