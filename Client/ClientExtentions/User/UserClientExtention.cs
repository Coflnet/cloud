namespace Coflnet.Client {
    public class UserClientExtention : IRegisterCommands
    {
        public void RegisterCommands(CommandController controller)
        {
            controller.RegisterCommand<Coflnet.Client.RegisteredUser>();
			controller.RegisterCommand<LoginUserResponse>();
        }
    }
}