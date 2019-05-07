

namespace Coflnet.Client
{
	/// <summary>
	/// Setup controller handles device setup, registration and tutorials.
	/// </summary>
	public class SetupStartController
	{

		public static SetupStartController Instance;


		//public GameObject tutorialScreen;
		//public GameObject startScreen;



		static SetupStartController()
		{
			Instance = new SetupStartController();
		}

		public SetupStartController()
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

