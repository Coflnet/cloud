using System.Collections;
using System.Collections.Generic;
using Coflnet;
using MessagePack;

namespace Coflnet {
	/// <summary>
	/// Can't be a ServerCommand because no user yet exists
	/// </summary>
	public class RegisterUser : Command {
		public override void Execute (CommandData data) {
			RegisterUserRequest request = data.GetAs<RegisterUserRequest> ();

			// validate captcha Token
			// todo :)

			CoflnetUser user = CoflnetUser.Generate (request.clientId, data.CoreInstance.EntityManager);
			user.PrivacySettings = request.privacySettings;

			var response = new RegisterUserResponse ();
			response.id = user.Id;
			response.secret = user.Secret;


			data.SendBack (CommandData.CreateCommandData<RegisteredUser, RegisterUserResponse> (data.SenderId, response));
			//SendTo(data.sId, user.PublicId, "createdUser");
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug => "registerUser";
	}


    public class CreateUser : CreationCommand
    {
        public override string Slug => "createuser";

        public override Entity CreateResource(CommandData data)
        {
			CreateUserRequest request = data.GetAs<CreateUserRequest> ();
			var user = new CoflnetUser(data.SenderId);
			user.PrivacySettings = request.privacySettings;
            return user;
        }


		[MessagePackObject]
		public class CreateUserRequest : CreationCommand.CreationParamsBase
		{
			[Key (1)]
			public Dictionary<string, bool> privacySettings;
		}
    }

	

    [MessagePackObject]
	public class RegisterUserRequest {
		[Key (0)]
		public string captchaToken;
		[Key (1)]
		public EntityId clientId;
		[Key (2)]
		public Dictionary<string, bool> privacySettings;
		/// <summary>
		/// Temporary assigned local id
		/// </summary>
		[Key (3)]
		public EntityId localId;
	}

	[MessagePackObject]
	public class RegisterUserResponse {
		[Key (0)]
		public EntityId id;
		[Key (1)]
		public byte[] secret;
		[Key (2)]
		public List<long> managingServers;
	}
}