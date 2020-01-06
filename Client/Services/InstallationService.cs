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
		/// Generates a new Installation
		/// </summary>
		public void Setup(SourceReference DeviceId)
		{
			ConfigController.InstallationId = clientCoreInstance.CreateResource<RegisterInstallation>(DeviceId).Id;
			// assign id
			clientCoreInstance.Id = ConfigController.InstallationId;
		}

		/// <summary>
		/// The Id of the current active software installation
		/// </summary>
		/// <value>Device Id</value>
		public SourceReference CurrentInstallId
		{
			get
			{
				if(ConfigController.InstallationId == default(SourceReference))
				{
					Setup(ConfigController.DeviceId);
				} else if(ConfigController.InstallationId.IsLocal)
				{
					// update the device id to the server generated one
					ConfigController.InstallationId = clientCoreInstance.ReferenceManager.
													GetResource<Installation>(ConfigController.DeviceId).Id;
				}
				return ConfigController.InstallationId;
			}
		}
	}
}


