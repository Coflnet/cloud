using Coflnet;

namespace Coflnet
{
	/// <summary>
	/// Receiveable resource.
	/// Represents a <see cref="Referenceable"/> that is capeable of receiving commands on its own
	/// </summary>
	public abstract class ReceiveableResource : Referenceable
	{
		public override void ExecuteCommand(MessageData data)
		{
			// each incoming command will be forwarded to the resource
			try
			{
				base.ExecuteCommand(data);
			}
			catch (CommandUnknownException)
			{

			}

			CoflnetCore.Instance.SendCommand(data);
		}
	}
}


