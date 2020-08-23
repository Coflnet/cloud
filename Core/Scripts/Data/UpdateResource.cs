namespace Coflnet
{
	public partial class EntityManager
	{
		public class UpdateEntityCommand : Command
		{
			/// <summary>
			/// Execute the command logic with specified data.
			/// </summary>
			/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
			public override void Execute (CommandData data)
			{
				data.CoreInstance.EntityManager.UpdateEntity (data.GetAs<CommandData> (), data.SenderId);
			}

			/// <summary>
			/// Special settings and Permissions for this <see cref="Command"/>
			/// /// </summary>
			/// <returns>The settings.</returns>
			protected override CommandSettings GetSettings ()
			{
				return new CommandSettings ();
			}
			/// <summary>
			/// The globally unique slug (short human readable id) for this command.
			/// </summary>
			/// <returns>The slug .</returns>
			public override string Slug => "UpdateEntity";
		}
	}

}