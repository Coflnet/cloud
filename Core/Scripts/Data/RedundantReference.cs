using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet
{
    /// <summary>
    /// Redundant reference Data exists on one or more servers (recomended)
    /// </summary>
    [DataContract]
	public class RedundantReference<T> : Reference<T> where T : Referenceable {
		[DataMember]
		protected List<long> failoverServers = new List<long> ();

		/// <summary>
		/// Adds a new server for extra redundancy.
		/// </summary>
		/// <param name="server">Server to add.</param>
		public void AddServer (CoflnetServer server) {
			AddServer (server.Id.ServerId);
		}

		/// <summary>
		/// Adds a new server for extra redundancy.
		/// </summary>
		/// <param name="serverId">Server to add.</param>
		public void AddServer (long serverId) {
			failoverServers.Add (serverId);
		}

		public void RemoveServer (CoflnetServer server) {
			RemoveServer (server.Id.ServerId);
		}
		/// <summary>
		/// Removes the server from the failover list.
		/// </summary>
		/// <param name="serverId">Server identifier.</param>
		public void RemoveServer (long serverId) {
			failoverServers.Remove (serverId);
		}

		/// <summary>
		/// Executes a command on the server containing the resource referenced by this object
		/// </summary>
		/// <param name="data">Command data to send</param>
		public new void ExecuteForResource (MessageData data) {
			CoflnetCore.Instance.SendCommand (data, ReferenceId.ServerId);
			// also send it to the failover servers
			foreach (var item in failoverServers) {
				CoflnetCore.Instance.SendCommand (data, item);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Coflnet.Server.RedundantReference`1"/> s Resource is lokally available.
		/// </summary>
		/// <value><c>true</c> if is lokal; otherwise, <c>false</c>.</value>
		[IgnoreDataMember]
		public bool IsLokal {
			get {

				return ReferenceManager.Instance.Exists (this.ReferenceId);
			}
		}

		/// <summary>
		/// Gets the nearest server which has a copy of this resource.
		/// </summary>
		/// <value>The nearest server.</value>
		[IgnoreDataMember]
		public CoflnetServer NearestServer {
			get {
				CoflnetServer closest = ServerController.Instance.GetOrCreate (ReferenceId.ServerId);
				foreach (var item in failoverServers) {
					var server = ServerController.Instance.GetOrCreate (item);
					// if another server that is closer or as fast as the managing server use it instead
					if (server.PingTimeMS <= closest.PingTimeMS)
						closest = server;
				}
				return closest;
			}
		}

		public RedundantReference () { }

		public RedundantReference (T resource, params CoflnetServer[] failoverServer) : base (resource) {
			if (failoverServer == null)
				return;
			foreach (var item in failoverServer) {
				this.failoverServers.Add (item.Id.ServerId);
			}
		}
	}
}