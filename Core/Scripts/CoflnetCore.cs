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

		/// <summary>
		/// Instance of an <see cref="ReferenceManager"> used for executing commands
		/// </summary>
		/// <value></value>
		public ReferenceManager ReferenceManager{get;set;}


		/// <summary>
		/// Is set to the correct server or client version of the core when calling the 
		/// `Init` method on the correct `Core` eg. `ClientCore`
		/// </summary>
		/// <value>a</value>
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
			protected set
			{
				_instance = value;
			}
		}

		/// <summary>
		/// Receives and processes a command.
		/// Counterpart to <see cref="SendCommand"/>
		/// </summary>
		/// <param name="data">Command data received from eg. the network</param>

		/// <param name="sender">The vertified sender of the command, controls if the command is executed right away or only sent to the managing server</param>
		public void ReceiveCommand(MessageData data, SourceReference sender = default(SourceReference))
		{
			this.ReferenceManager.ExecuteForReference(data,sender);
		}


		public abstract void SendCommand(MessageData data, long serverId = 0);
		/// <summary>
		/// Sends a command.
		/// </summary>
		/// <param name="receipient">Receipient to send to.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="id">Unique Identifier of the message.</param>
		/// <param name="sender">Optional sender, if not sent the core will attempt to replace with active Referenceable eg CoflnetUser.</param>
		/// <typeparam name="C"><see cref="Command"/> to send.</typeparam>
		/// <typeparam name="T">Type of <paramref name="data"/> needed for seralization.</typeparam>
		public abstract void SendCommand<C, T>(SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference)) where C : Command;
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
			SendCommand<C,T>(receipient,data,default(SourceReference),callback);
		}


		/// <summary>
		/// Sends a command that returns a value, allows a callback to be passed.
		/// </summary>
		/// <param name="receipient">Receipient to send to.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="sender">Identity under which to send the command.</param>
		/// <param name="callback">Callback to be executed when the response is received.</param>
		/// <typeparam name="C"><see cref="Command"/> to send.</typeparam>
		/// <typeparam name="T">Type of <paramref name="data"/> needed for seralization.</typeparam>
		public void SendCommand<C, T>(SourceReference receipient, T data, SourceReference sender, Command.CommandMethod callback) where C : ReturnCommand
		{
			long id = ThreadSaveIdGenerator.NextId;
			ReturnCommandService.Instance.AddCallback(id, callback);
			SendCommand<C, T>(receipient, data, id,sender);
		}

	}
}
