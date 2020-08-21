using System.Linq;

namespace Coflnet.Server
{
	public class LoginUser : Coflnet.LoginUser
	{

		public override void Execute(CommandData data)
		{
			var serverMessage = data as ServerCommandData;
			if (serverMessage == null || serverMessage.Connection == null)
			{
				throw new CoflnetException("connection_invalid", "Nothing connected");
			}

			var options = serverMessage.GetAs<LoginParams>();


			var user = data.CoreInstance.EntityManager.GetEntity<CoflnetUser>(options.id);

			if (user.Secret == null || options.secret == null || !user.Secret.SequenceEqual(options.secret))
			{
				throw new CoflnetException("secret_invalid", "The users secret is incorrect");
			}
						serverMessage.Connection.User = user;

			var response = CommandData.CreateCommandData<LoginUserResponse, EntityId>(user.Id, user.Id);
			response.SenderId = data.CoreInstance.Id;
			data.SendBack(response);
		}
	}

}


