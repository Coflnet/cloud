using Coflnet;
using Coflnet.Core;


namespace Coflnet.Client
{
	public class AppDataService
	{

		public static AppDataService Instance { get; }

		public ClientCore ClientCoreInstance {get;set;}

		static AppDataService()
		{
			Instance = new AppDataService();
		}

		public RemoteDictionary<string,string> Storage
		{
			get 
			{
				var dataId = UserService.Instance.CurrentUser.appData[ConfigController.ApplicationSettings.id].ReferenceId;
				return ClientCoreInstance.ReferenceManager.GetResource<ApplicationData>(dataId)
					.KeyValues;
			}
		}
	}

}

