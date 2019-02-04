using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using Coflnet.Server;

namespace Coflnet.Server
{

	public class DeviceController
	{
		private ConcurrentDictionary<SourceReference, Device> devices;
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

		public bool Exists(SourceReference id)
		{
			return devices.ContainsKey(id);
		}

		public Device GetDevice(SourceReference id)
		{
			Device device;
			devices.TryGetValue(id, out device);
			return device;
		}

		public void SendToDevice(MessageData data, SourceReference deviceId)
		{
			GetDevice(deviceId).UnsentMessages.Add(data);
		}
	}




}
