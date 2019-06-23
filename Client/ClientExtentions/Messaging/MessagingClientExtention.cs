using Coflnet.Client.Messaging;

namespace Coflnet.Client {
    public class MessagingClientExtention : IRegisterCommands
    {
        public void RegisterCommands(CommandController controller)
        {
			controller.RegisterCommand<ChatMessageCommand>();
        }
    }
}