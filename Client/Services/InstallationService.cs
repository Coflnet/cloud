using Coflnet;
using Coflnet.Client;


namespace Coflent.Client
{
	/// <summary>
	/// Manages the current <see cref="Installation"/>
	/// </summary>
	public class InstallService  {

		public static InstallService Instance;

		/// <summary>
		/// The <see cref="ClientCore"/> to use as the active core
		/// </summary>
		public ClientCore clientCoreInstance;

		static InstallService()
		{
			Instance = new InstallService();
		}

		public InstallService(ClientCore coreInstance)
		{
			this.clientCoreInstance = coreInstance;
		}

		public InstallService() : this(ClientCore.ClientInstance)
		{

		}

		/// <summary>
		/// Tries to optain a new Device id
		/// </summary>
		public void Setup()
		{
			ConfigController.DeviceId = clientCoreInstance.CreateResource<RegisterInstallation>().Id;
		}

		/// <summary>
		/// The Id of the current active software installation
		/// </summary>
		/// <value>Device Id</value>
		public SourceReference CurrentInstallId
		{
			get
			{
				if(ConfigController.DeviceId == default(SourceReference))
				{
					Setup();
				} else if(ConfigController.DeviceId.IsLocal)
				{
					// update the device id to the server generated one
					ConfigController.InstallationId = ClientCore.Instance.ReferenceManager.
													GetResource<Installation>(ConfigController.DeviceId).Id;
				}
				return ConfigController.InstallationId;
			}
		}
	}
}


