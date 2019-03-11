

namespace Coflnet.Server
{
	/// <summary>
	/// Cloud module.
	/// Registers Commands for interacting with other servers
	/// </summary>
	public class CloudModule : IRegisterCommands
	{
		public void RegisterCommands(CommandController controller)
		{
			controller.RegisterCommand<LoginServer>();
		}
	}
}