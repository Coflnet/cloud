using System.Collections;
using System.Collections.Generic;
using Coflnet;
using MessagePack;

namespace Coflnet {
	/// <summary>
	/// Can't be a ServerCommand because no user yet exists
	/// </summary>
	public class RegisterUser : Command {
		public override void Execute (MessageData data) {
			RegisterUserRequest request = data.GetAs<RegisterUserRequest> ();

			// validate captcha Token
			// todo :)

			CoflnetUser user = CoflnetUser.Generate (request.clientId, data.CoreInstance.ReferenceManager);
			user.PrivacySettings = request.privacySettings;

			var response = new RegisterUserResponse ();
			response.id = user.Id;
			response.secret = user.Secret;

			UnityEngine.Debug.Log ($"received from {data.sId}");

			data.SendBack (MessageData.CreateMessageData<RegisteredUser, RegisterUserResponse> (data.sId, response));
			//SendTo(data.sId, user.PublicId, "createdUser");
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug => "registerUser";
	}

	[MessagePackObject]
	public class RegisterUserRequest {
		[Key (0)]
		public string captchaToken;
		[Key (1)]
		public SourceReference clientId;
		[Key (2)]
		public Dictionary<string, bool> privacySettings;
		/// <summary>
		/// Temporary assigned local id
		/// </summary>
		[Key (3)]
		public SourceReference localId;
	}

	[MessagePackObject]
	public class RegisterUserResponse {
		[Key (0)]
		public SourceReference id;
		[Key (1)]
		public byte[] secret;
		[Key (2)]
		public List<long> managingServers;
	}
}