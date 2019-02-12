using System.Collections.Generic;
using System.Runtime.Serialization;
using System;

namespace Coflnet
{

	[DataContract]
	public class Device : Referenceable
	{
		private static CommandController commandController;

		/// <summary>
		/// Wherether or not this is the primary device
		/// Deprecated, the first item of the users device list is his primary device
		/// </summary>
		//[DataMember]
		//private bool primary;
		/// <summary>
		/// Public Key is used for authetication purposes
		/// </summary>
		[DataMember]
		private byte[] publicKey;
		/// <summary>
		/// Device secret used for authentication purposes
		/// </summary>
		[DataMember]
		private string secret;
		/// <summary>
		/// The device model, used for helping the user remember which device this is
		/// </summary>
		[DataMember]
		private string model;
		/// <summary>
		/// If access was granted contains the current installed apps (applicationId) we know of
		/// </summary>
		[DataMember]
		private List<string> installedApps;
		/// <summary>
		/// users using this device
		/// </summary>
		[DataMember]
		private List<Reference<CoflnetUser>> users;
		/// <summary>
		/// All IP-Adresses under which this device has connected until now
		/// </summary>
		[DataMember]
		private List<string> ipAdresses;
		/// <summary>
		/// The latesst location of the device
		/// </summary>
		[DataMember]
		private LocationInfo location;
		/// <summary>
		/// The device resolution
		/// </summary>
		[DataMember]
		private Resolution resolution;
		/// <summary>
		/// If this device should receive pushNotifications
		/// </summary>
		[DataMember]
		private bool receiveNotifications;
		/// <summary>
		/// When revoked device has to be authenticated again to access the user
		/// </summary>
		[DataMember]
		private bool revoked;


		/// <summary>
		/// Messages in sending queue
		/// </summary>
		private List<MessageData> unsentMessages;




		public byte[] PublicKey
		{
			get
			{
				return publicKey;
			}
		}

		public string Secret
		{
			get
			{
				return secret;
			}
		}

		public string Model
		{
			get
			{
				return model;
			}
		}

		public List<string> InstalledApps
		{
			get
			{
				return installedApps;
			}
		}

		public List<Reference<CoflnetUser>> Users
		{
			get
			{
				return users;
			}
		}

		public List<string> IpAdresses
		{
			get
			{
				return ipAdresses;
			}
		}

		public LocationInfo Location
		{
			get
			{
				return location;
			}
		}

		public Resolution Resolution
		{
			get
			{
				return resolution;
			}
		}

		public bool ReceiveNotifications
		{
			get
			{
				return receiveNotifications;
			}
		}

		public bool Revoked
		{
			get
			{
				return revoked;
			}
		}

		public List<MessageData> UnsentMessages
		{
			get
			{
				return unsentMessages;
			}
		}

		public Device(SourceReference owner) : base(owner)
		{
		}



		public Device() { }

		public override CommandController GetCommandController()
		{
			return commandController;
		}
	}

	/// <summary>
	/// Device/monitor resolution
	/// </summary>
	public class Resolution
	{
		private int _Width;

		private int _Height;

		private int _RefreshRate;

		/// <summary>
		///   <para>Resolution width in pixels.</para>
		/// </summary>
		public int width
		{
			get
			{
				return this._Width;
			}
			set
			{
				this._Width = value;
			}
		}



		/// <summary>
		/// Resolution height in pixels.
		/// </summary>
		/// <value>Resolution height in pixels.</value>
		public int height
		{
			get
			{
				return this._Height;
			}
			set
			{
				this._Height = value;
			}
		}

		/// <summary>
		/// Refresh rate in Hz, 0 if unknown.
		/// </summary>
		/// <value>Resolution's vertical refresh rate in Hz.</value>
		public int refreshRate
		{
			get
			{
				return this._RefreshRate;
			}
			set
			{
				this._RefreshRate = value;
			}
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:Coflnet.Resolution"/>.
		/// </summary>
		/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:Coflnet.Resolution"/> with the format "width x height @ refreshRateHz.</returns>
		public override string ToString()
		{
			return String.Format("{0} x {1} @ {2}Hz", this._Width, this._Height, this._RefreshRate);
		}
	}

	/// <summary>
	/// Location information for a place on earth.
	/// </summary>
	public struct LocationInfo
	{
		private double _timestamp;

		private float _latitude;

		private float _longitude;

		private float _altitude;

		private float _horizontalAccuracy;

		private float _verticalAccuracy;

		/// <summary>
		/// Geographical device location latitude
		/// </summary>
		/// <value>The latitude.</value>
		public float latitude
		{
			get
			{
				return this._latitude;
			}
		}

		/// <summary>
		/// Geographical device location latitude.
		/// </summary>
		/// <value>The longitude.</value>
		public float longitude
		{
			get
			{
				return this._longitude;
			}
		}

		/// <summary>
		/// Geographical device location altitude.
		/// </summary>
		/// <value>The altitude.</value>
		public float altitude
		{
			get
			{
				return this._altitude;
			}
		}

		/// <summary>
		/// Horizontal accuracy of the location.
		/// </summary>
		/// <value>The horizontal accuracy.</value>
		public float horizontalAccuracy
		{
			get
			{
				return this._horizontalAccuracy;
			}
		}

		/// <summary>
		/// Vertical accuracy of the location.
		/// </summary>
		/// <value>The vertical accuracy.</value>
		public float verticalAccuracy
		{
			get
			{
				return this._verticalAccuracy;
			}
		}

		/// <summary>
		/// Timestamp (in seconds since 1970) when location was captured.
		/// </summary>
		/// <value>The timestamp.</value>
		public double timestamp
		{
			get
			{
				return this._timestamp;
			}
		}
	}
}