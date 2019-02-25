namespace Coflnet.Server
{
	public class LoginServer : Command
	{
		public override void Execute(MessageData data)
		{
			var loginParams = data.GetAs<LoginParams>();
			if (loginParams == null)
			{

			}
			var serverData = data as ServerMessageData;



			//loginParams.


			serverData.Connection.AuthenticatedIds.Add(loginParams.id);
		}

		public override CommandSettings GetSettings()
		{
			return new CommandSettings();
		}

		public override string GetSlug()
		{
			return "loginServer";
		}
	}
}