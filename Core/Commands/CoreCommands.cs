using Coflnet;


namespace Coflnet.Core
{
    public class CoreCommands : IRegisterCommands
    {
        public void RegisterCommands(CommandController controller)
        {
            controller.RegisterCommand<RegisterDevice>();
			controller.RegisterCommand<ReceiveConfirm>();
        }
    }
}


