

namespace Coflnet.Core.DeviceCommands
{
    /// <summary>
    /// Adds an user to the device
    /// </summary>
    public class AddUserCommand : Command
	{
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute(MessageData data)
		{
			data.GetTargetAs<Device>().Users.Add(new Reference<CoflnetUser>(data.GetAs<SourceReference>()));
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		public override CommandSettings GetSettings()
		{
			return new CommandSettings(true,true,false,false,IsManagingServerOrSelfPermission.Instance );
		}
		/// <summary>
		/// The globally unique slug (short human readable id) for this command.
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug => "addUser";
	}


	/// <summary>
    /// Removes an user to the device
    /// </summary>
    public class RemoveUserCommand : Command
	{
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute(MessageData data)
		{
			data.GetTargetAs<Device>().Users.Remove(new Reference<CoflnetUser>(data.GetAs<SourceReference>()));
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		public override CommandSettings GetSettings()
		{
			return new CommandSettings(true,true,false,false,IsManagingServerOrSelfPermission.Instance );
		}
		/// <summary>
		/// The globally unique slug (short human readable id) for this command.
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug => "removeUser";
	}
}