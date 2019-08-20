

using Coflnet.Core.User;

namespace Coflnet.Core{

	public class CoreExtentions  {

		public static Coflnet.IRegisterCommands[] Commands =
		{
			new CoreCommands(),
			new UserCoreExtention()
		};
	}

}
