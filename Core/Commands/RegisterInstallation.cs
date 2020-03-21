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

        public override Referenceable CreateResource(MessageData data)
        {
			UnityEngine.Debug.Log("creating install on " + data.CoreInstance.GetType().Name);
			var install = new Installation ();
			install.Device = new Reference<Device>(data.rId);
            return install;
        }

		public override string Slug => "createInstall";
	}

}