using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Coflnet;
using UnityEngine;

namespace Coflnet.Server.Master {

	public class MasterController : IRegisterCommands {
		private List<CoflnetServerSize> servers;

		public void RegisterCommands (CommandController controller) {
			controller.RegisterCommand<RegisterNewServer> ();
		}

		public class RegisterNewServer : Command {
			public override void Execute (MessageData data) {
				RegisterRequest request = data.GetAs<RegisterRequest> ();

			}

			protected override CommandSettings GetSettings () {
				return new CommandSettings ();
			}

			public override string Slug => "registerNewServer";

		}
	}

	/// <summary>
	/// Used for registering as a new server on a master server
	/// </summary>
	[DataContract]
	public class RegisterRequest {
		[DataMember]
		public string Ip;
		[DataMember]
		public byte[] PublicKey;
		[DataMember]
		public List<CoflnetServer.ServerRole> AvailableRoles;
		[DataMember]
		public Benchmark Benchmark;
		/// <summary>
		/// Optional secret used to secure the network
		/// </summary>
		[DataMember]
		public byte[] RegisterSecret;
	}

	public class RegisterResponse {
		public long id;
		public byte[] signature;
		public List<CoflnetServer.ServerRole> assingedRoles;
	}

	public class CoflnetServerSize : CoflnetServer {
		protected Benchmark benchmark;
		public CoflnetServerSize (long pId, string ip, byte[] publicKey, List<ServerRole> roles, ServerState state, Benchmark benchmark) : base (pId, ip, publicKey, roles, state) {
			this.benchmark = benchmark;
		}
	}

	[DataContract]
	public class Benchmark {
		[DataMember]
		protected long ram;
		[DataMember]
		protected int cores;
		[DataMember]
		protected long hearts;
		[DataMember (Name = "ds")]
		protected long discSpace;
		[DataMember (Name = "rs")]
		protected long readSpead;
		[DataMember (Name = "ws")]
		protected long writeSpead;
		[DataMember (Name = "csd")]
		protected long connectionSpeedUp;
		[DataMember (Name = "csu")]
		protected long connectionSpeedDown;

	}

}

namespace Coflnet.Server {
	public class IsServerPermission : Permission {
		private static readonly IsServerPermission instance;
		public override string Slug => "isServer";

		public override bool CheckPermission (MessageData data, Referenceable target) {
			return true;
		}

		static IsServerPermission () {
			instance = new IsServerPermission ();
		}

		public static Permission Instance {
			get {
				return instance;
			}
		}
	}

	public class MatchPermission : Permission {
		private object value;
		/// <summary>
		/// Checks the permission.
		/// </summary>
		/// <returns><c>true</c>, if first element in the array and the value match, <c>false</c> otherwise.</returns>
		/// <param name="options">Options.</param>
		public override bool CheckPermission (MessageData data, Referenceable target) {
			return true; //options[0].Equals(value);
		}

		public override string Slug => "match";

		public MatchPermission (object value) {
			this.value = value;
		}
	}

}