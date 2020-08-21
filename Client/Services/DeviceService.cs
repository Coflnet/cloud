using Coflnet;
using Coflnet.Client;


namespace Coflent.Client
{
	public class DeviceService  {

		public static DeviceService Instance;

		/// <summary>
		/// The <see cref="ClientCore"/> to use as the active core
		/// </summary>
		public ClientCore clientCoreInstance;

		static DeviceService()
		{
			Instance = new DeviceService();
		}

		public DeviceService(ClientCore coreInstance)
		{
			this.clientCoreInstance = coreInstance;
		}

		public DeviceService() : this(ClientCore.ClientInstance)
		{

		}

		/// <summary>
		/// Tries to optain a new Device id
		/// </summary>
		public void Setup()
		{
			ConfigController.DeviceId = clientCoreInstance.CreateEntity<RegisterDevice>().Id;
		}

		/// <summary>
		/// The Id of the current active device running the software
		/// </summary>
		/// <value>Device Id</value>
		public EntityId CurrentDeviceId
		{
			get
			{
				if(ConfigController.DeviceId == default(EntityId))
				{
					Setup();
				} else if(ConfigController.DeviceId.IsLocal)
				{
					// update the device id to the server generated one
					ConfigController.DeviceId = clientCoreInstance.EntityManager.
													GetEntity<Device>(ConfigController.DeviceId).Id;
				}
				return ConfigController.DeviceId;
			}
		}
	}
}


