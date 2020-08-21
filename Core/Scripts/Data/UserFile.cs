using System;
using System.Collections.Generic;
using MessagePack;

namespace Coflnet
{

	public class UserFile : Entity
	{
		protected Application application;
		/// <summary>
		/// Access is a number that grants other app users and the public access to a file
		///              * in the order of public,other app users,friends
		///              * 1 being read and 2 being read and write
		///              * the higher the int the greater the permission
		///              * eg. 012
		///              * everybody else can't access
		///              * app users can read
		///              * friends can read and write
		///              *
		///              * 100 everybody (including friends can read)
		///              * 000 only the uploader can read and write
		/// </summary>
		[Obsolete("got replaced by the Entity.Access Property")]
		protected int access;



		/// <summary>
		/// The unix timestamp when this file will be deleted.
		/// </summary>
		protected long deleteAt;
		/// <summary>
		/// Optinal licence for this file
		/// </summary>
		protected Licence licence;
		/// <summary>
		/// The main server to load this file from
		/// </summary>
		protected CoflnetServer server;
		/// <summary>
		/// In case the main server is not reachable the file can also be found on this server
		/// Not every file has a failover server or just one with HDD for cost reasons
		/// </summary>
		protected CoflnetServer failoverServer;
		/// <summary>
		/// This server is a desaster backup server in case the first two servers fail the file will still be available on it.
		/// However it is compressed on an HDD so access is very slow.
		/// </summary>
		protected CoflnetServer backupServer;
		/// <summary>
		/// The size in bytes of this file
		/// </summary>
		protected long size;



		Application Application
		{
			get
			{
				return application;
			}
		}


		long DeleteAt
		{
			get
			{
				return deleteAt;
			}
		}

		Licence Licence
		{
			get
			{
				return licence;
			}
		}

		CoflnetServer Server
		{
			get
			{
				return server;
			}
		}

		CoflnetServer FailoverServer
		{
			get
			{
				return failoverServer;
			}
		}

		CoflnetServer BackupServer
		{
			get
			{
				return backupServer;
			}
		}

		public override CommandController GetCommandController()
		{
			throw new NotImplementedException();
		}
	}


	public class Licence : Entity
	{
		protected string identifier;
		protected string text;
		/// <summary>
		/// In case the holder is not an user of us
		/// </summary>
		protected string holderName;
		protected int year;

		protected static Dictionary<string, Licence> licenses;

		public Licence GetLicence(string identifier)
		{
			if (!licenses.ContainsKey(identifier))
			{
				throw new Exception("No licence with this identifier found");
			}
			return licenses[identifier];
		}

		public override CommandController GetCommandController()
		{
			return new CommandController();
		}

		public Licence(string identifier, string text, string holderName, int year)
		{
			if (licenses.ContainsKey(identifier))
			{
				throw new Exception("There allready is a licence with the identifier " + identifier);
			}
			this.identifier = identifier;
			this.text = text;
			this.holderName = holderName;
			this.year = year;

			licenses.Add(identifier, this);
		}
	}


}