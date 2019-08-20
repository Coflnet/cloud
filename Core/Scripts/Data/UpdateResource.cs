namespace Coflnet
{
    public partial class ReferenceManager {
        public class UpdateResourceCommand : Command
			{
				/// <summary>
				/// Execute the command logic with specified data.
				/// </summary>
				/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
				public override void Execute(MessageData data)
				{
					data.CoreInstance.ReferenceManager.UpdateResource(data.GetAs<MessageData>(),data.sId);
				}
		
				/// <summary>
				/// Special settings and Permissions for this <see cref="Command"/>
				/// </summary>
				/// <returns>The settings.</returns>
				public override CommandSettings GetSettings()
				{
					return new CommandSettings();
				}
				/// <summary>
				/// The globally unique slug (short human readable id) for this command.
				/// </summary>
				/// <returns>The slug .</returns>
				public override string Slug => "UpdateResource";
			}
	}


}