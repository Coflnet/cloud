namespace Coflnet.Core.DeviceCommands
{
    /// <summary>
    /// Executed when a devie detects newly installed apps
    /// </summary>
    public class DeviceInstalledCommand : Command
	{
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute(MessageData data)
		{
			data.GetTargetAs<Device>().InstalledApps.AddRange(data.GetAs<string[]>());
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		protected override CommandSettings GetSettings()
		{
			return new CommandSettings(false,true,false,true,IsManagingServerOrSelfPermission.Instance );
		}
		/// <summary>
		/// The globally unique slug (short human readable id) for this command.
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug => "installedApps";
	}
}