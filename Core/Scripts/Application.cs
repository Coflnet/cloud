using System;

namespace Coflnet
{
	public class Application : Referenceable
	{
		public CommandController commandController
		{
			get;
			private set;
		}


		public string Name
		{
			get;
			private set;
		}

		// returns a different command controller for every instance
		public override CommandController GetCommandController()
		{
			return commandController;
		}
	}
}


