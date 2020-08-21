namespace Coflnet
{
    /// <summary>
    /// Creates a new Installation on The current Server
    /// </summary>
    public class RegisterInstallation : CreationCommand {
		protected override CommandSettings GetSettings () {
			// everyone can register devices
			return new CommandSettings ();
		}

        public override Entity CreateResource(CommandData data)
        {
			var install = new Installation ();
			install.Device = new Reference<Device>(data.Recipient);
            return install;
        }

		public override string Slug => "createInstall";
	}

}