using System.Collections.Generic;
using MessagePack;

namespace Coflnet
{
	/// <summary>
	/// Represents a serverside Application code that is capeable of managing resources or other tasks
	/// </summary>
	[MessagePackObject]
	public class Application : Referenceable
	{
		[IgnoreMember]
		public CommandController commandController
		{
			get;
			private set;
		}

		[Key(5)]
		public HashSet<string> ClientCommands
		{
			get;set;
		}

		[Key(0)]
		public string Name
		{
			get;
			private set;
		}

		/// <summary>
		/// The server identifiers this application is hosted on
		/// </summary>
		[Key(1)]
		public List<long> serverIds;

		// returns a different command controller for every instance
		public override CommandController GetCommandController()
		{
			return commandController;
		}
	}
}


