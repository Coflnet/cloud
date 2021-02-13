

using System.Collections.Generic;
using Coflnet.Core.User;
using Core.Extentions.KeyValue;

namespace Coflnet.Core{

	public class CoreExtentions  {

		public static List<Coflnet.IRegisterCommands> Commands = new List<IRegisterCommands>()
		{
			new CoreCommands(),
			new UserCoreExtention(),
			new KeyValueExtension()
		};
	}

}
