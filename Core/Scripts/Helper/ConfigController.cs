using System;
using System.Collections.Generic;
using MessagePack;

namespace Coflnet
{
	public static class ConfigController
	{
		private static ApplicationSettings applicationSettings;

		public static EntityId DeviceId;
		public static EntityId InstallationId;

		public static EntityId ActiveUserId = EntityId.Default;

		public static UserSettings UserSettings
		{
			get
			{
				if(Users == null || Users.Count == 0){
					Users =new List<UserSettings>();
					Users.Add(new UserSettings(){userId = ActiveUserId});
				}
				return Users.Find(u => u.userId == ActiveUserId);
			}
		}

		public static List<UserSettings> Users { get; private set; }

		public static EntityId ManagingServer
		{
			get
			{
				return ApplicationSettings.id;
			}
		}


		/// <summary>
		/// Settings for the current application
		/// </summary>
		/// <value>The application settings.</value>
		public static ApplicationSettings ApplicationSettings
		{
			get
			{
				if (applicationSettings == null)
					applicationSettings = new ApplicationSettings();
				return applicationSettings;
			}
		}

		public enum WebProtocol
		{
			https = 443,
			http = 80,
			wss = 8080,
			ws = 8008,
			ftp = 21
		}


		static ConfigController()
		{
			try 
			{
				Load();
			} catch(Exception e)
			{
				Logger.Error($"Failed to load config. Error message: '{e.Message}'");
			}

			

			AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => Save();
		}

		private static void Load()
		{
			if (ValuesController.HasKey("currentUserIdentifier"))
			{
				ActiveUserId = ValuesController.GetValue<EntityId>("currentUserIdentifier");
			}

			if (ValuesController.HasKey("deviceId"))
			{
				DeviceId = ValuesController.GetValue<EntityId>("deviceId");
			}
			if (ValuesController.HasKey("installId"))
			{
				InstallationId = ValuesController.GetValue<EntityId>("installationId");
			}

			if (FileController.Exists("userSettings"))
				Users = MessagePackSerializer.Deserialize<List<UserSettings>>(FileController.ReadAllBytes("userSettings"));
			else
				Users = new List<UserSettings>();
		}


		/// <summary>
		/// Gets the full URL to some endpoint. (prepends the path with a server domain/ip)
		/// </summary>
		/// <returns>The URL.</returns>
		/// <param name="path">Path.</param>
		public static string GetUrl(string path, WebProtocol protocol = WebProtocol.https, long serverId = 0)
		{
			var protocolName = protocol.ToString();// Enum.GetName(typeof(WebProtocol), protocol);
			return $"{protocolName}://{GetUrl((ushort)protocol,serverId)}/{path.TrimStart('/')}";
		}

		public static string GetUrl(UInt16 port,long serverId =0)
		{
			if(serverId== 0)
			{
				serverId = GetManagingServerId();
			}

			// serverId 1,1,* is reserved for development 
			// also region 0 is development / not set
			if (serverId <= (new EntityId(1, 1, 1, 0)).ServerId)
			{
				return $"localhost:{port}";
			}

			string serverName = BitConverter.ToString(
				BitConverter.GetBytes(serverId));

			return $"{serverName}.coflnet.com:{port}";
		}

		private static long GetManagingServerId()
		{
			if (UserSettings == null)
			{
				return ApplicationSettings.id.ServerId;
			}
			else
			{
				return UserSettings.managingServers[0];
			}
		}

		public static void Save()
		{
			FileController.WriteAllBytes("userSettings", MessagePackSerializer.Serialize(UserSettings));
			ValuesController.SetValue<EntityId>("currentUserIdentifier", ActiveUserId);
			ValuesController.SetValue<EntityId>("deviceId", DeviceId);
			ValuesController.SetValue<EntityId>("installationId", InstallationId);
		}

		public static long PrimaryServer
		{
			get
			{
				if (UserSettings.managingServers.Count == 0)
					throw new Exception("no managing server selected");
				return UserSettings.managingServers[0];
			}
		}
	}


	/// <summary>
	/// Out of cloud settings for users, to make sure private details are not synced
	/// </summary>
	[MessagePackObject]
	public class UserSettings
	{
		[Key(0)]
		public List<long> managingServers;
		[Key(1)]
		public EntityId userId;
		[Key(2)]
		public byte[] userSecret;
		[Key(3)]
		public string Locale;


		public UserSettings()
		{
			managingServers = new List<long>();
			managingServers.Add(0);
		}

		public UserSettings(List<long> managingServers, EntityId userId, byte[] userSecret, string locale = null)
		{
			this.managingServers = managingServers;
			this.userId = userId;
			this.userSecret = userSecret;
			Locale = locale;
		}
	}

	public class ApplicationSettings
	{
		public EntityId id;

		public long serverId;
	}


	public class ConfigService : CoflnetServiceBase
	{
		public EntityId ManagingServer {
			get 
			{
                return ConfigController.ManagingServer;
            }
		}
	}
}
