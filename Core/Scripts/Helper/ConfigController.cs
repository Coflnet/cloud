using System;
using System.Collections.Generic;
using MessagePack;

namespace Coflnet
{
	public static class ConfigController
	{
		private static ApplicationSettings applicationSettings;

		public static SourceReference ActiveUserId = SourceReference.Default;

		public static UserSettings UserSettings
		{
			get
			{
				return Users.Find(u => u.userId == ActiveUserId);
			}
		}

		public static List<UserSettings> Users { get; private set; }


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
			if (FileController.Exists("userSettings"))
				Users = MessagePackSerializer.Deserialize<List<UserSettings>>(FileController.ReadAllBytes("userSettings"));
			else
				Users = new List<UserSettings>();

			if (ValuesController.HasKey("currentUserIdentifier"))
			{
				ActiveUserId = ValuesController.GetValue<SourceReference>("currentUserIdentifier");
			}

			AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => Save();
		}


		/// <summary>
		/// Gets the full URL to some endpoint. (prepends the path with a server domain/ip)
		/// </summary>
		/// <returns>The URL.</returns>
		/// <param name="path">Path.</param>
		public static string GetUrl(string path, WebProtocol protocol = WebProtocol.https)
		{
			var protocolName = protocol.ToString();// Enum.GetName(typeof(WebProtocol), protocol);
			return $"{protocolName}://{GetUrl((ushort)protocol)}/{path.TrimStart('/')}";
		}

		public static string GetUrl(UInt16 port)
		{
			long serverId;
			if (UserSettings == null)
			{
				serverId = ApplicationSettings.id.ServerId;
			}
			else
			{
				serverId = UserSettings.managingServers[0];
			}

			// serverId 1,1,* is reserved for development 
			if (serverId == (new SourceReference(1, 1, 1, 0)).ServerId)
			{
				return $"localhost:{port}";
			}

			string serverName = BitConverter.ToString(
				BitConverter.GetBytes(serverId));

			return $"{serverName}.coflnet.com:{port}";
		}

		public static void Save()
		{
			FileController.WriteAllBytes("userSettings", MessagePackSerializer.Serialize(UserSettings));
			ValuesController.SetValue<SourceReference>("currentUserIdentifier", ActiveUserId);
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



	[MessagePackObject]
	public class UserSettings
	{
		[Key(0)]
		public List<long> managingServers;
		[Key(1)]
		public SourceReference userId;
		[Key(2)]
		public byte[] userSecret;
		[Key(3)]
		public string Locale;


		public UserSettings()
		{
			managingServers = new List<long>();
			managingServers.Add(0);
		}

		public UserSettings(List<long> managingServers, SourceReference userId, byte[] userSecret, string locale = null)
		{
			this.managingServers = managingServers;
			this.userId = userId;
			this.userSecret = userSecret;
			Locale = locale;
		}
	}

	public class ApplicationSettings
	{
		public SourceReference id;
	}
}
