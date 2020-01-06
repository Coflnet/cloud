using Coflnet;


namespace Coflnet.Core
{
    public class CoreCommands : IRegisterCommands
    {
        public void RegisterCommands(CommandController controller)
        {
            controller.RegisterCommand<RegisterDevice>();
            // device and Client needs to know the command 
            Device.globalCommands.OverwriteCommand<RegisterInstallation>();
			controller.RegisterCommand<ReceiveConfirm>();
        }
    }
}


