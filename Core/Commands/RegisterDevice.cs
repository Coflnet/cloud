namespace Coflnet
{
    public class RegisterDevice : CreationCommand {
		protected override CommandSettings GetSettings () {
			// everyone can register devices
			return new CommandSettings ();
		}

        public override Entity CreateResource(CommandData data)
        {
            return new Device ();
        }

        public override string Slug => "registerDevice";
	}

}