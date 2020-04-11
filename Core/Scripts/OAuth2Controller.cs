using System.Collections;
using System.Collections.Generic;
using RestSharp;
using Coflnet;

public class OAuth2Controller
{

	//private static string DataShifter.ufURL = "https://beta.coflnet.com";
	private static string publicAppID = "5005455241412926";
//	private static string appSecret = "";
	//private static string redirectUri = "";

	//private static string accessCode = "";


	//public delegate void ApiCallBack(string response, int statusCode);
	public delegate void RegisterCallback();

	public event RegisterCallback afterRegister;


	public static OAuth2Controller instance;



	static OAuth2Controller()
	{
		instance = new OAuth2Controller();
	}







	/// <summary>
	/// Gets the access token with a previously received code.
	/// </summary>
	/// <param name="client">Client.</param>
	/// <param name="user">User.</param>
	/// <param name="accessCode">Access code.</param>
	public void GetAccessTokenWithCode(ThirdPartyClient client, Coflnet.CoflnetUser user, string accessCode)
	{
		var request = new RestRequest(client.service.TokenPath);
		var restClient = new RestClient(client.service.GetUrl());

		request.AddHeader("grant_type", "authorization_code");
		request.AddHeader("client_id", client.id);
		request.AddHeader("client_secret", client.secret);
		request.AddHeader("code", accessCode);

		var response = restClient.Execute(request);

		if (response.StatusCode != System.Net.HttpStatusCode.OK)
		{
			throw new CoflnetException("oauth_failed", $"Oauth handshake failed, {client.service.Slug} responded with: `{response.Content}`");
		}


		var binary = MessagePack.MessagePackSerializer.ConvertFromJson(response.Content);
		var content = MessagePack.MessagePackSerializer.Deserialize<OAuthResponse>(binary);



		Oauth2Token token = new Oauth2Token(user, content.access_token,
											client.service,
											System.DateTime.Now.AddSeconds(content.expires_in),
											content.refresh_token);

		user.ThirdPartyTokens[client.service.Slug] = token;
	}




	public static int CurrentUnixTime()
	{
		System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
		return (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
	}
	/*


	// extra   
	public void GetInstallToken(bool forceRenew = false)
	{
		StartCoroutine(GetInstallID(forceRenew));
	}

    
	IEnumerator GetInstallID(bool forceRenew)
	{
		yield return new WaitForSeconds(1f);
		if (PlayerPrefs.HasKey("userSecret") && !forceRenew)
		{
			StartCoroutine(GetTokenWithInstallPassSend());
			yield break;
		}



		string url = DataShifter.ufURL + "/api/v1/user/new";
		WWWForm form = new WWWForm();
		form.AddField("client_id", publicAppID);
		form.AddField("client_secret", appSecret);
		WWW www = new WWW(url, form);
		yield return www;
		if (!string.IsNullOrEmpty(www.error))
		else
		{
			UserRegister response = JsonUtility.FromJson<UserRegister>(www.text);
			DataController.userId = response.user_id;
			PlayerPrefs.SetString("userId", response.user_id.ToString());
			PlayerPrefs.SetString("userSecret", response.user_secret);
			StartCoroutine(GetTokenWithInstallPassSend());
		}
	}






	IEnumerator GetTokenWithInstallPassSend()
	{
		string toOpen = DataShifter.ufURL;
		toOpen += "/oauth2/access_token";

		// Add the required parameters
		WWWForm form = new WWWForm();
		form.AddField("grant_type", "password");
		form.AddField("client_id", publicAppID);
		form.AddField("client_secret", appSecret);
		form.AddField("scope", "full_access");
		form.AddField("username", PlayerPrefs.GetString("userId"));
		form.AddField("password", PlayerPrefs.GetString("userSecret"));

		WWW www = new WWW(toOpen, form);
		yield return www;
		if (!string.IsNullOrEmpty(www.error))
		else
		{
			OAuthResponse response = JsonUtility.FromJson<OAuthResponse>(www.text);
			PlayerPrefs.SetString("user_access_token", response.access_token);
			PlayerPrefs.SetString("user_refresh_token", response.refresh_token);
			PlayerPrefs.SetInt("user_expire_time", CurrentUnixTime() + response.expires_in);

			// we can now get Enc Keys for this user
			this.GetComponent<SocketController>().GetEncKey(true, true);

			// invoke any subscribed events
			afterRegister.Invoke();
		}
	}
    */


}

public class OAuthResponse
{
	public string error;
	public string token_type;
	public int expires_in;
	public string access_token;
	public string refresh_token;
}