using System;
using System.Collections.Generic;
using MessagePack;

namespace Coflnet
{
	public static class ConfigController
	{
		private static ApplicationSettings applicationSettings;

		public static UserSettings UserSettings { get; private set; }
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
				UserSettings = MessagePackSerializer.Deserialize<UserSettings>(FileController.ReadAllBytes("userSettings"));
			else
				UserSettings = new UserSettings();
			AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => Save();
		}


		/// <summary>
		/// Gets the full URL to some endpoint. (prepends the path with a server domain/ip)
		/// </summary>
		/// <returns>The URL.</returns>
		/// <param name="path">Path.</param>
		public static string GetUrl(string path, WebProtocol protocol = WebProtocol.https)
		{
			if (UserSettings == null)
				return null;
			var protocolName = protocol.ToString();// Enum.GetName(typeof(WebProtocol), protocol);
			return $"{protocolName}://{GetUrl((ushort)protocol)}/{path.TrimStart('/')}";
		}

		public static string GetUrl(UInt16 port)
		{
			long serverId = UserSettings.managingServers[0];

			// serverId 0 is reserved for development 
			if (serverId == 0)
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
	}

	public class ApplicationSettings
	{
		public SourceReference id;
	}
}
