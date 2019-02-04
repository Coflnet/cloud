
using UnityEngine;
using UnityEngine.UI;



namespace Coflnet.Client
{
	/// <summary>
	/// Setup controller handles device setup, registration and tutorials.
	/// </summary>
	public class SetupStartController
	{

		public static SetupStartController Instance;
		public GameObject tutorialScreen;
		public GameObject startScreen;



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
		private void Setup()
		{
			if (ValuesController.HasKey("setupCompleted"))
				return;



			ValuesController.SetInt("setupCompleted", 1);
		}
	}
}

