using System;
using System.Collections.Generic;
using RestSharp;

namespace Coflnet
{

	public interface PushNotificationProvider
	{
		/// <summary>
		/// Sends a notification.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <param name="body">Body.</param>
		/// <param name="extraData">Extra data (key value pairs).</param>
		void SendNotification(string title, string body, Dictionary<string, string> extraData, PushNotificationToken token);

		/// <summary>
		/// Cancles a notification.
		/// </summary>
		/// <param name="id">Notification Identifier.</param>
		/// <param name="token">Token.</param>
		void CancleNotification(string id, PushNotificationToken token);

	}


	public class FirebasePushNotification : PushNotificationProvider
	{
		private static readonly string uri = "https://fcm.googleapis.com/fcm/send";
		//private static readonly WebClient client = new WebClient();


		public void CancleNotification(string id, PushNotificationToken token)
		{
			throw new NotImplementedException();
		}

		public void SendNotification(string title, string body, Dictionary<string, string> extraData, PushNotificationToken token)
		{
			//client.DownloadDataAsync("https://beta.coflnet.com");
			//client.DownloadDataCompleted += (object sender, DownloadDataCompletedEventArgs e) =>
			//{
			//    Logger.Log(Encoding.UTF8.GetString(e.Result));
			// };

		}
	}


	public class PushNotificationToken : ThirdPartyToken
	{
		protected PushNotificationProvider provider;
		protected List<string> notificationIds;

		public PushNotificationToken(CoflnetUser user, string token, PushNotificationProvider provider, List<string> notificationIds) : base(user, token)
		{
			this.provider = provider;
			this.notificationIds = notificationIds;
		}

		public PushNotificationToken(CoflnetUser user, string token, PushNotificationProvider provider) : base(user, token)
		{
			this.provider = provider;
			this.notificationIds = new List<string>();
		}

		PushNotificationProvider Provider
		{
			get
			{
				return provider;
			}
		}

		List<string> NotificationIds
		{
			get
			{
				return notificationIds;
			}
		}
	}

	public interface IThirdPartyApi
	{
		/// <summary>
		/// Usualy the domain 
		/// </summary>
		/// <returns>The slug.</returns>
		string Slug{get;}

		string GetDescription();

		string Execute(RestRequest request, ThirdPartyToken token);
	}

	/// <summary>
	/// Third party services are external providers that can do special services.
	/// </summary>
	public abstract class ThirdPartyService : IThirdPartyApi
	{
		/// <summary>
		/// The ip adresses/ urls under which the service is reachable
		/// </summary>
		protected List<string> urls;

		protected Protocol protocol = Protocol.https;
		protected enum Protocol
		{
			https,
			websocket,
			tcp
		}

		/// <summary>
		/// Returns a valid url under which this service is accessable
		/// </summary>
		/// <returns>The an URL.</returns>
		public string GetUrl()
		{
			return urls.GetRandom();
		}

		public abstract string AuthorizePath { get; }

		public abstract string TokenPath { get; }
		/// <summary>
		/// Should return a new Token and refresh token as well as expiration time
		/// </summary>
		/// <returns>The token.</returns>
		/// <param name="refreshToken">Refresh token.</param>
		public abstract OAuthResponse RefreshToken(string refreshToken);

		public string AuthorizeUrl
		{
			get
			{
				return GetUrl() + AuthorizePath;
			}
		}

		public string TokenUrl
		{
			get
			{
				return GetUrl() + TokenPath;
			}
		}


		public abstract string Slug{get;}

		public abstract string GetDescription();

		/// <summary>
		/// Execute the specified request with the token.
		/// </summary>
		/// <returns>The execute.</returns>
		/// <param name="request">Request.</param>
		/// <param name="token">Token.</param>
		public abstract string Execute(RestRequest request, ThirdPartyToken token);
	}



	public class ThirdPartyClient
	{
		public string id;
		public string secret;
		public ThirdPartyService service;

		public ThirdPartyClient(string id, string secret, ThirdPartyService service)
		{
			this.id = id;
			this.secret = secret;
			this.service = service;
		}
	}



	public class ThirdPartyToken
	{
		protected CoflnetUser user;
		protected string token;
		protected IThirdPartyApi party;

		public ThirdPartyToken(CoflnetUser user, string token)
		{
			this.user = user;
			this.token = token;
		}

		public ThirdPartyToken(CoflnetUser user, string token, IThirdPartyApi party)
		{
			this.user = user;
			this.token = token;
			this.party = party;
		}

		CoflnetUser User
		{
			get
			{
				return user;
			}
		}

		string Token
		{
			get
			{
				return token;
			}
			set
			{
				token = value;
			}
		}
	}


	public class Oauth2Token : ThirdPartyToken
	{
		public DateTime expiration;
		public string refreshToken;

		public Oauth2Token(CoflnetUser user, string token, IThirdPartyApi party, DateTime expiration, string refreshToken) : base(user, token, party)
		{
			this.expiration = expiration;
			this.refreshToken = refreshToken;
		}
	}


	public static class PseudoRandom
	{
		static System.Random random;

		static PseudoRandom()
		{
			random = new System.Random();
		}

		public static int Next(int max)
		{
			return random.Next(0, max);
		}

		/// <summary>
		/// Gets a random element from a list.
		/// </summary>
		/// <returns>The random.</returns>
		/// <param name="list">List.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static T GetRandom<T>(this List<T> list)
		{
			return list[Next(list.Count)];
		}
	}

}