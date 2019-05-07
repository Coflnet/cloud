using System;

namespace Coflnet
{
	/// <summary>
	/// Provides core functionallity for the coflnet ServerSystem
	/// Allows to reuse code server and client side.
	/// Instance is set to the correct server or client version of the core.
	/// </summary>
	public abstract class CoflnetCore : Referenceable
	{
		private static CoflnetCore _instance;

		public static CoflnetCore Instance
		{
			get
			{
				if (_instance == null)
				{
					throw new Exception("No instance has been set yet");
				}
				return _instance;
			}
			set
			{
				_instance = value;
			}
		}




		public abstract void SendCommand(MessageData data, long serverId = 0);
		/// <summary>
		/// Sends a command.
		/// </summary>
		/// <param name="receipient">Receipient to send to.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="id">Unique Identifier of the message.</param>
		/// <typeparam name="C"><see cref="Command"/> to send.</typeparam>
		/// <typeparam name="T">Type of <paramref name="data"/> needed for seralization.</typeparam>
		public abstract void SendCommand<C, T>(SourceReference receipient, T data, long id = 0) where C : Command;
		public abstract void SendCommand<C>(SourceReference receipient, byte[] data) where C : Command;


		/// <summary>
		/// Sends a command that returns a value, allows a callback to be passed.
		/// </summary>
		/// <param name="receipient">Receipient to send to.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="callback">Callback to be executed when the response is received.</param>
		/// <typeparam name="C"><see cref="Command"/> to send.</typeparam>
		/// <typeparam name="T">Type of <paramref name="data"/> needed for seralization.</typeparam>
		public void SendCommand<C, T>(SourceReference receipient, T data, Command.CommandMethod callback) where C : ReturnCommand
		{
			long id = ThreadSaveIdGenerator.NextId;
			ReturnCommandService.Instance.AddCallback(id, callback);
			SendCommand<C, T>(receipient, data, id);

		}
	}
}
