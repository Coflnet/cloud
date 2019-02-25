
namespace Coflnet.Client
{
	public class RegisteredUser : Coflnet.RegisteredUser
	{
		public override void Execute(MessageData data)
		{
			var response = data.GetAs<RegisterUserResponse>();
			ConfigController.UserSettings.userId = response.id;
			ConfigController.UserSettings.userSecret = response.secret;

			// Login
			CoflnetClient.Instance.SendCommand<LoginUser, LoginParams>(new SourceReference(response.id.ServerId, 0),
																	  new LoginParams(response.id, response.secret));
		}

	}
}


