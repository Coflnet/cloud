using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using Coflnet.Server;

namespace Coflnet.Server
{

	public class DeviceController
	{
		private ConcurrentDictionary<EntityId, Device> devices;
		public static DeviceController instance;


		static DeviceController()
		{
			instance = new DeviceController();
		}

		/// <summary>
		/// Adds a device.
		/// </summary>
		/// <param name="device">Device.</param>
		public void AddDevice(Device device)
		{
			devices.TryAdd(device.Id, device);
		}

		public bool Exists(EntityId id)
		{
			return devices.ContainsKey(id);
		}

		public Device GetDevice(EntityId id)
		{
			Device device;
			devices.TryGetValue(id, out device);
			return device;
		}

		public void SendToDevice(CommandData data, EntityId deviceId)
		{
			GetDevice(deviceId).UnsentMessages.Add(data);
		}
	}




}
