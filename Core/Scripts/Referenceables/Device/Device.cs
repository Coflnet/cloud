using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using MessagePack;
using Coflnet.Core.Commands;
using Coflnet.Core.DeviceCommands;
using Coflnet.Core;

namespace Coflnet
{

    [DataContract]
	public class Device : ReceiveableResource
	{
		public static CommandController commandController {get;}

		/// <summary>
		/// What application type this device coresponds to
		/// </summary>
		[DataMember]
		public EntityId ApplicationId;
		/// <summary>
		/// Device secret used for authentication purposes
		/// </summary>
		[DataMember]
		public string Secret;
		/// <summary>
		/// The device model, used for helping the user remember which device this is
		/// </summary>
		[DataMember]
		public string Model;
		/// <summary>
		/// If access was granted contains the current installed apps (applicationId) we know of
		/// </summary>
		[DataMember]
		public List<string> InstalledApps;
		/// <summary>
		/// users using this device
		/// </summary>
		[DataMember]
		public List<Reference<CoflnetUser>> Users;
		/// <summary>
		/// All IP-Adresses under which this device has connected until now
		/// </summary>
		[DataMember]
		public List<string> IpAdresses;
		/// <summary>
		/// The latesst location of the device
		/// </summary>
		[DataMember]
		public RemoteObject<LocationInfo> Location;
		/// <summary>
		/// The device resolution
		/// </summary>
		[DataMember]
		public Resolution Resolution;
		/// <summary>
		/// If this device should receive pushNotifications
		/// </summary>
		[DataMember]
		public bool ReceiveNotifications;
		/// <summary>
		/// When revoked device has to be authenticated again to access the user
		/// </summary>
		[DataMember]
		public bool Revoked;


		/// <summary>
		/// Messages in sending queue
		/// </summary>
		public List<CommandData> UnsentMessages;




		public Device(EntityId owner) : base(owner)
		{
			
		}



		public Device() { }

		static Device()
		{
			commandController = new CommandController(persistenceCommands);
			commandController.RegisterCommand<DeviceInstalledCommand>();
			//commandController.RegisterCommand<AddUserCommand>();
			commandController.RegisterCommand<RemoveUserCommand>();

			// Add commands for the users list
			RemoteList<Reference<CoflnetUser>>.AddCommands
				(commandController,nameof(Users),m=>m.GetTargetAs<Device>().Users
				,m=>new Reference<CoflnetUser>(m.GetAs<EntityId>()));
				
			// Add commands for the installed apps list
			RemoteList<string>.AddCommands
				(commandController,nameof(InstalledApps),m=>m.GetTargetAs<Device>().InstalledApps);
		}

		public override CommandController GetCommandController()
		{
			return commandController;
		}
	}

	[DataContract]
    public class Installation : ReceiveableResource
    {
		private static CommandController _commands;

		[DataMember]
		public EntityId AppId;
		[DataMember]
		public Reference<Device> Device;
		[DataMember]
		public Reference<ApplicationData> AppData;

		static Installation()
		{
			_commands = new CommandController(persistenceCommands);
			_commands.RegisterCommand<CreateUser>();
		}

        public override CommandController GetCommandController()
        {
            return _commands;
        }
    }


    /// <summary>
    /// Command that each device receives when the user has received a command (distribute)
    /// </summary>
    public class DeviceDistributeCommand : Command
	{
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
		public override void Execute(CommandData data)
		{
			// execute it on the local user
			data.CoreInstance.ReceiveCommand(data.GetAs<CommandData>());
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		protected override CommandSettings GetSettings()
		{
			return new CommandSettings( );
		}
		/// <summary>
		/// The globally unique slug (short human readable id) for this command.
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug => "usercmd";
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
	[MessagePackObject]
	public struct LocationInfo
	{
		/// <summary>
		/// Geographical device location latitude
		/// </summary>
		/// <value>The latitude.</value>
		[Key(0)]
		public float latitude
		{
			get;set;
		}

		/// <summary>
		/// Geographical device location latitude.
		/// </summary>
		/// <value>The longitude.</value>
		[Key(1)]
		public float longitude
		{
			get;set;
		}

		/// <summary>
		/// Geographical device location altitude.
		/// </summary>
		/// <value>The altitude.</value>
		[Key(2)]
		public float altitude
		{
			get;set;
		}

		/// <summary>
		/// Horizontal accuracy of the location.
		/// </summary>
		/// <value>The horizontal accuracy.</value>
		[Key(3)]
		public float horizontalAccuracy
		{
			get;set;
		}

		/// <summary>
		/// Vertical accuracy of the location.
		/// </summary>
		/// <value>The vertical accuracy.</value>
		[Key(4)]
		public float verticalAccuracy
		{
			get;set;
		}

		/// <summary>
		/// Timestamp (in seconds since 1970) when location was captured.
		/// </summary>
		/// <value>The timestamp.</value>
		[Key(5)]
		public double timestamp
		{
			get;set;
		}
	}
}