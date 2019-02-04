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




		public abstract void SendCommand(MessageData data);
		public abstract void SendCommand<C, T>(SourceReference receipient, T data) where C : Command;
		public abstract void SendCommand<C>(SourceReference receipient, byte[] data) where C : Command;

	}
}
