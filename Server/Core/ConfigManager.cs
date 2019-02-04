using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coflnet.Config;
using MessagePack;
using System.IO;


public class ConfigManager : MonoBehaviour
{
	protected CoflnetConfig config;
	public static readonly string configPath = "/home/ekwav/serverConfig.json";

	void Awake()
	{

	}


	public void LoadConfig()
	{
		string json = File.ReadAllText(configPath);
		config = MessagePack.MessagePackSerializer.Deserialize<CoflnetConfig>(MessagePack.MessagePackSerializer.FromJson(json));
	}
}


namespace Coflnet.Config
{
	using System.Runtime.Serialization;
	using System.Security.Cryptography.X509Certificates;
	/// <summary>
	/// Holds all options for configuration
	/// </summary>
	[DataContract]
	public class CoflnetConfig
	{
		public SSLConfig sslConfig;
		public CoflnetServer serverConfig;
	}

	/// <summary>
	/// options for ssl
	/// </summary>
	[DataContract]
	public class SSLConfig
	{
		[DataMember]
		public string pfxPath;
		[DataMember]
		public string pfxPassword;
		[IgnoreDataMember]
		public X509Certificate certificate;
	}

	[DataContract]
	public class ServerConfig
	{
		[DataMember]
		public string publicId;
		[DataMember]
		public List<CoflnetServer.ServerRole> roles;
	}

	/// <summary>
	/// Tracking config for piwik Backend
	/// </summary>
	[DataContract]
	public class TrackingConfig
	{
		[DataMember]
		public string uri;
		[DataMember]
		public string userId;
	}
}
