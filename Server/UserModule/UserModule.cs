using Coflnet;

namespace Coflnet.Server
{
	public class UserModule : IRegisterCommands
	{
		public void RegisterCommands(CommandController controller)
		{
			controller.RegisterCommand<LoginUser>();
		}
	}
}
