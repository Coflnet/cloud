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
				throw new CoflnetException("connection_invalid", "Nothing connected");
			}

			var options = serverMessage.GetAs<LoginParams>();


			var user = data.CoreInstance.ReferenceManager.GetResource<CoflnetUser>(options.id);

			if (user.Secret == null || options.secret == null || !user.Secret.SequenceEqual(options.secret))
			{
				throw new CoflnetException("secret_invalid", "The users secret is incorrect");
			}
			UnityEngine.Debug.Log("authentiated");
			serverMessage.Connection.User = user;

			var response = MessageData.CreateMessageData<LoginUserResponse, SourceReference>(user.Id, user.Id);
			response.sId = data.CoreInstance.Id;
			data.SendBack(response);
		}
	}

}


