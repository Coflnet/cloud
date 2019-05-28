using Coflnet.Server;

namespace Coflnet.Server
{
	/// <summary>
	/// This class is here to load extra command modules into the server
	/// This serves as the main extention point of the coflnet server
	/// </summary>
	public class ExtraModules
	{
		public static IRegisterCommands[] Commands =
		{
			new UserModule()
		};

	}
}
