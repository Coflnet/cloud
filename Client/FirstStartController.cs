
using System;
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
			// make an install if we haven't yet
			if(ConfigController.InstallationId == default(SourceReference))
			{
				InstallService.Instance.Setup(ConfigController.DeviceId);
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

	/// <summary>
	/// Throws an Repeatexception until an online id is granted
	/// </summary>
	class OnlineIdResolver
	{
		private SourceReference _id;

		public SourceReference Id
		{
			get 
			{
				if(_id.IsLocal)
				{
					throw new ReapeatExecutionException(new TimeSpan(0,0,5));
				}
				return _id;
			}
			set
			{
				_id = value;
			}
		}
	}

	/// <summary>
	/// Tells the core that the currect action should be repeated
	/// </summary>
    public class ReapeatExecutionException : CoflnetException
    {
		/// <summary>
		/// The Time to wait until execution should be tried again.
		/// Will be increased by 20% each new time execution fails (incremental backoff)
		/// </summary>
		/// <value></value>
		public TimeSpan WaitTime {get;}

        public ReapeatExecutionException(TimeSpan waitTime, string userMessage = null, int responseCode = 0, string info = null, long msgId = -1) : base("repeat", $"The action should be repeated in {waitTime}", userMessage, responseCode, info, msgId)
        {
        }
    }
}

