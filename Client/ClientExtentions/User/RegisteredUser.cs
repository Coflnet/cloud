
namespace Coflnet.Client
{
	public class RegisteredUser : Coflnet.RegisteredUser
	{
		public override void Execute(MessageData data)
		{
			var response = data.GetAs<RegisterUserResponse>();



			UnityEngine.Debug.Log("new id : " + response.id);


			// all outdated the client core is a device not an user


			data.CoreInstance.ReferenceManager
			.UpdateIdAndAddRedirect(data.rId,response.id);

			// add the core behind
			var user = data.GetTargetAs<CoflnetUser>();

			user.GetCommandController()
				.AddBackfall(data.CoreInstance.GetCommandController());

			// The core itself also has the same id
			data.CoreInstance.Id = response.id;
			

			//ConfigController.UserSettings.userId = response.id;
			ConfigController.Users.Add(new UserSettings(response.managingServers, response.id, response.secret));
			
			// if this is the first user, activate it
			if(ConfigController.Users.Count == 1)
				ConfigController.ActiveUserId = response.id;


			// Login
			ClientCore.Instance.SendCommand<LoginUser, LoginParams>(new SourceReference(response.id.ServerId, 0),
																	  new LoginParams(response.id, response.secret));
		}

	}
}


