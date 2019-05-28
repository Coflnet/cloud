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


		public override Command ExecuteCommand(MessageData data)
		{
			UnityEngine.Debug.Log("running receivable");
			// each incoming command will be forwarded to the resource
			try
			{
				var command = base.ExecuteCommand(data);
				UnityEngine.Debug.Log($"ohh man");
				if (command.Settings.IsChaning)
				{
					UnityEngine.Debug.Log($"sending command ");
					CoflnetCore.Instance.SendCommand(data);
				}
				return command;
			}
			catch (CommandUnknownException e)
			{
				UnityEngine.Debug.Log($"didn't find Command {e.Slug} ");
			}
			return null;
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

		/// <summary>
		/// Will send the provided Command and replace the <see cref="MessageData.sId"/> with the id of the resource
		/// </summary>
		/// <param name="data"></param>
		public void SendCommand(MessageData data){
			data.sId = this.Id;
			CoflnetCore.Instance.SendCommand(data);
		}
	}
}


