using System.Linq;

namespace Coflnet.Server
{
	public class LoginUser : Coflnet.LoginUser
	{

		public override void Execute(MessageData data)
		{
			var serverMessage = data as ServerMessageData;
			if (serverMessage == null || serverMessage.Connection == null)
			{
				throw new CoflnetException("connection_invalid", "");
			}

			var options = serverMessage.GetAs<LoginParams>();


			var user = ReferenceManager.Instance.GetResource<CoflnetUser>(options.id);

			if (user.Secret == null || options.secret == null || !user.Secret.SequenceEqual(options.secret))
			{
				throw new CoflnetException("secret_invalid", "The users secret is incorrect");
			}
			UnityEngine.Debug.Log("authentiated");
			serverMessage.Connection.User = user;
			// tell the socket that a user connected
		}
	}

}


