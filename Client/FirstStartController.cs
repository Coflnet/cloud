
using Coflent.Client;

namespace Coflnet.Client
{
	/// <summary>
	/// Setup controller handles device setup, registration and tutorials.
	/// </summary>
	public class FirstStartSetupController
	{

		public static FirstStartSetupController Instance;


		//public GameObject tutorialScreen;
		//public GameObject startScreen;



		static FirstStartSetupController()
		{
			Instance = new FirstStartSetupController();
		}

		public FirstStartSetupController()
		{


		}


		/// <summary>
		/// Only executes once on startup 
		/// unless setup is aborded or an error occurs
		/// </summary>
		public void Setup()
		{
			if (ValuesController.HasKey("setupCompleted"))
				return;

			// get a device Id if we haven't yet
			if(ConfigController.DeviceId == default(SourceReference))
			{
				DeviceService.Instance.Setup();
			}

			UnityEngine.Debug.Log("doing setup :)");

			PrivacyService.Instance.ShowScreen(Done);
		}

		public void Done()
		{
			//ReferenceManager.Instance.GetResource<CoflnetUser>(new SourceReference()).
			UserService.Instance.CreateUser(PrivacyService.Instance.Settings);


			ValuesController.SetInt("setupCompleted", 1);
		}

		public void RedoSetup(bool afterRestart)
		{
			ValuesController.DeleteKey("setupCompleted");
		}
	}


}

