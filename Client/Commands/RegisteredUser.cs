
namespace Coflnet.Client
{
	public class RegisteredUser : Coflnet.RegisteredUser
	{
		public override void Execute(MessageData data)
		{
			var response = data.GetAs<RegisterUserResponse>();
			//ConfigController.UserSettings.userId = response.id;
			ConfigController.Users.Add(new UserSettings(response.managingServers, response.id, response.secret));
			//ConfigController.UserSettings.userSecret = response.secret;

			// Login
			ClientCore.Instance.SendCommand<LoginUser, LoginParams>(new SourceReference(response.id.ServerId, 0),
																	  new LoginParams(response.id, response.secret));
		}

	}
}


