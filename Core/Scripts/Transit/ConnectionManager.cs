
using Coflnet;
namespace Coflnet.Core.Scripts
{
	/// <summary>
	/// Manages <see cref="ICommandTransmit"/> To other <see cref="Device"/> or <see cref="CoflnetServer"/>
	/// </summary>
	public class ConnectionManager  {
		/// <summary>
		/// An instance of <see cref="ConnectionManager"/> class since usually only one is required.
		/// </summary>
		public static ConnectionManager Instance;
		static ConnectionManager () {
			Instance = new ConnectionManager ();
		}

		public ICommandTransmit GetOrCreateConnectionTo(EntityId id)
		{
			return ClientSocket.Instance;
		}
	}
}
