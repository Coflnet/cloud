using System;
using System.Diagnostics;
using RestSharp;
using Coflnet;

public class Track
{

	static string trackingServerURL = "https://track.coflnet.com/?rec=1&send_image=0";
	string url;

	string lastRequest = "";



	public static Track instance;

	static Track()
	{
		instance = new Track();
	}

	public void Error(string type, string data, string error)
	{
#if UNITY_EDITOR
		UnityEngine.Debug.LogError(type + data + " - " + error);
#endif
		SendTrackingRequest("Error/" + type + " / Stacktrace:" + error + " data: " + data);
	}

	public void SendTrackingRequest(string title, bool sendSinceStartup = false)
	{
		var client = new RestClient(ConfigController.GetUrl("track"));

		var request = new RestRequest("resource/{id}", Method.GET);
		request.AddParameter("url", title); // adds to POST or URL querystring based on Method
		request.AddParameter("title", title);
		request.AddParameter("action_name", title);
		request.AddParameter("idsite", "coflnet.com");

#if UNITY_64
		// if we have a screen width
		request.AddParameter("res", UnityEngine.Screen.width.ToString() + "x" + UnityEngine.Screen.height);
#endif



		// send of
		client.ExecuteAsync(request, response =>
		{
			// done
		});

		if (sendSinceStartup)
			url += GetStartTimeParameter();

		//if (PrivacyController.privacyLevel > 1)
		//	url += GetVisitorID();
		//url += GetCurrentLevel();
		//Debug.Log(url);
		if (lastRequest == url)
			return;
		lastRequest = url;
#if !UNITY_EDITOR
	//	StartCoroutine(SendRealRequest(url));
#endif
	}




	private double GetStartTimeParameter()
	{
		return (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds;
	}


}
