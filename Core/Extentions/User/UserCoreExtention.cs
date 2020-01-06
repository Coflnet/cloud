using Coflnet;

namespace Coflnet.Core.User {

    public class UserCoreExtention : IRegisterCommands
    {
        public void RegisterCommands(CommandController controller)
        {
            controller.RegisterCommand<RegisterUser>();
            // part of the core
            //controller.RegisterCommand<RegisterDevice>();
        }
    }
}

