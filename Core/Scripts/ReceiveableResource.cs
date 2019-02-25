using Coflnet;

namespace Coflnet
{
	/// <summary>
	/// Receiveable resource.
	/// Represents a <see cref="Referenceable"/> that is capeable of receiving commands on its own
	/// </summary>
	public abstract class ReceiveableResource : Referenceable
	{
		protected static CommandController persistenceCommands;


		static ReceiveableResource()
		{
			persistenceCommands = new CommandController();
			persistenceCommands.RegisterCommand<GetMessages>();
		}


		public override void ExecuteCommand(MessageData data)
		{
			UnityEngine.Debug.Log("running receivable");
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

		public ReceiveableResource(SourceReference owner) : base(owner)
		{
		}

		public ReceiveableResource() : base()
		{
		}


		public class GetMessages : Command
		{
			public override void Execute(MessageData data)
			{
				foreach (var item in MessageDataPersistence.Instance.GetMessagesFor(data.rId))
				{
					data.SendBack(item);
				}
			}

			public override CommandSettings GetSettings()
			{
				return new CommandSettings(IsSelfPermission.Instance);
			}

			public override string GetSlug()
			{
				return "getMessages";
			}
		}
	}
}


