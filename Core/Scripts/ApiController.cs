using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using RestSharp;
using Coflnet;

public class ApiController
{

	// Settings
	public readonly bool executeCallbackOnError = false;
	public readonly bool trackErrors = true;


	// end Settings



	public delegate void ApiCallBack(string response, System.Net.HttpStatusCode statusCode);

	public static ApiController instance;
	private static string playerPrefsKey = "apiRequests";

	private List<UploadObject> uploads;

	/// <summary>
	/// Error handler executed after logging the error.
	/// </summary>
	public delegate void ErrorHandler(string userMessage);
	public ErrorHandler defaultErrorHanlder;
	private Dictionary<String, ErrorHandler> errorHandlers = new Dictionary<string, ErrorHandler>();

	public enum RequestType
	{
		GET,
		POST,
		PUT,
		DELETE,
		HEAD,
		OPTIONS,
		PATCH,
		MERGE,
		COPY
	};

	static ApiController()
	{
		instance = new ApiController();
	}

	public ApiController()
	{

		uploads = new List<UploadObject>();
	}


	private void LoadRequests()
	{
		
		/*
		if (!SecurePlayerPrefs.HasKey(playerPrefsKey))
			return;
		UploadSaveObject uploadSave = JsonUtility.FromJson<UploadSaveObject>(SecurePlayerPrefs.GetString(playerPrefsKey));
		if (uploadSave.uploads == null)
			uploads = new List<UploadObject>();
		else
			uploads = new List<UploadObject>(uploadSave.uploads);
		Debug.Log("uploads to take " + SecurePlayerPrefs.GetString(playerPrefsKey));
		PerformPersitedUpload();
*/
	}

	public void PerformPersitedUpload()
	{
		foreach (var upload in uploads)
		{
			SendToApi(upload.url, RemoveFromPersist, upload.variables, upload.requestType);
		}
	}

	public void GetFromApi(string url, ApiCallBack callback = null)
	{
		//StartCoroutine (GetFromApiAsync (url, callback));
		SendToApi(url, callback, new ServerPostVariable("", ""), RequestType.GET);
	}


	/// <summary>
	/// Sends POST request with user access token to API.
	/// </summary>
	/// <param name="url">URL.</param>
	/// <param name="callback">Callback, can be null for ignoring.</param>
	public void SendToApi(string url, ApiCallBack callback = null, RequestType type = RequestType.POST)
	{
		SendToApiAsync(url, callback, null, type);
	}


	/// <summary>
	/// Sends POST request with user access token to API.
	/// </summary>
	/// <param name="url">URL.</param>
	/// <param name="callback">Callback, can be null for ignoring.</param>
	/// <param name="variable">Variable.</param>
	public void SendToApi(string url, ApiCallBack callback = null, ServerPostVariable variable = null, RequestType type = RequestType.POST)
	{
		ServerPostVariable[] variables = { variable };
		SendToApiAsync(url, callback, variables, type);
	}

	/// <summary>
	/// Sends POST request with user access token to API.
	/// </summary>
	/// <param name="url">URL.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="variables">Variables.</param>
	public void SendToApi(string url, ApiCallBack callback = null, ServerPostVariable[] variables = null, RequestType type = RequestType.POST)
	{
		SendToApiAsync(url, callback, variables, type);
	}


	/// <summary>
	/// Sends a request with user access token to API.
	/// </summary>
	/// <param name="url">URL.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="json">Variables.</param>
	public void SendToApi(string url, ApiCallBack callback, string json, RequestType type = RequestType.POST)
	{
		SendToApiAsync(url, callback, null, type, json);
	}




	/// <summary>
	/// Sends POST request with user access token to API.
	/// </summary>
	/// <param name="path">Path relative to the domain or complete url, decission is taken by inspecting the string if it contains a '://' or not.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="variables">Variables.</param>
	public void SendToApiAsync(string path, ApiCallBack callback = null, ServerPostVariable[] variables = null, RequestType type = RequestType.POST, string jsonString = "")
	{
#if UNITY_WEBGL
        return;
#endif


		string url = ConfigController.GetUrl(path);

		if (path.Contains("://"))
			url = path;



		var client = new RestClient(url);
		string accessToken = PlayerPrefs.GetString("user_access_token");
		client.Authenticator = new RestSharp.Authenticators.
			OAuth2AuthorizationRequestHeaderAuthenticator(accessToken);
		//client.Authenticato
		// client.Authenticator = new HttpBasicAuthenticator(username, password);

		var request = new RestRequest((Method)type);

		foreach (var item in variables)
		{
			request.AddParameter(item.key, item.value);
		}

		// execute the request
		IRestResponse response = client.Execute(request);
		var content = response.Content; // raw content as string


		// easy async support
		client.ExecuteAsync(request, r =>
		{
			if (r.StatusCode != System.Net.HttpStatusCode.OK)
			{
				string text = r.Content;
				if (text.Contains("Attention Required! | Cloudflare") && text.Contains("CAPTCHA"))
				{
					//NotificationHandler.instance.ShowNotification("error", "bad_ip");
					throw new Exception("Cloudflare blocked this ip");
				}

				HandleError(text, r.StatusCode, url);
			}
			else if (callback != null)
			{
				callback(r.Content, r.StatusCode);
			}
			else
			{
				throw new Exception("no callback given to API request");
			}
		});


	}

	/// <summary>
	/// Handles an API error.
	/// </summary>
	/// <param name="response">Response from the API.</param>
	/// <param name="responseCode">API response code indicating what went wrong</param>
	/// <param name="url">Url to the endpoint</param>
	private void HandleError(string response, System.Net.HttpStatusCode responseCode, string url)
	{
		ErrorResponse errorResponse = null;
		try
		{
			errorResponse = JsonUtility.FromJson<ErrorResponse>(response);
		}
		catch (Exception)
		{
			Debug.LogError("Error while fetching '" + url + "': " + response);
			return;
		}


		Debug.LogError("CoflnetAPI: " + errorResponse.message);
		Track.instance.Error("API", errorResponse.message, errorResponse.error);
		if (errorHandlers == null || errorResponse.error == null)
		{
			return;
		}
		if (errorHandlers.ContainsKey(errorResponse.error))
		{
			errorHandlers[errorResponse.error].Invoke(errorResponse.user_message);
		}
		else if (defaultErrorHanlder != null)
		{
			defaultErrorHanlder.Invoke(errorResponse.user_message);
		}
	}

	/// <summary>
	/// Adds a new error handler.
	/// </summary>
	/// <param name="slug">Slug.</param>
	/// <param name="errorHandler">Error handler.</param>
	public void RegisterErrorHandler(string slug, ErrorHandler errorHandler)
	{
		if (errorHandlers.ContainsKey(slug))
		{
			throw new Exception("There is allready an errorHandler for this slug, use OverwriteErrorHandler instead.");
		}
		errorHandlers.Add(slug, errorHandler);
	}

	/// <summary>
	/// Overwrites an error handler.
	/// </summary>
	/// <param name="slug">Error slug wich to overwrite handler for.</param>
	/// <param name="errorHandler">The new error handler.</param>
	public void OverwriteErrorHandler(string slug, ErrorHandler errorHandler)
	{
		errorHandlers[slug] = errorHandler;
	}

	/// <summary>
	/// Sets the default error handler inoked if there is no error specific handler
	/// </summary>
	/// <param name="errorHandler">Error handler to invoke.</param>
	public void SetDefaultErrorHandler(ErrorHandler errorHandler)
	{
		defaultErrorHanlder = errorHandler;
	}

	public WWWForm GetWWWForm(ServerPostVariable[] variables)
	{
		// add the variables
		WWWForm form = new WWWForm();
		if (variables != null)
			foreach (var variable in variables)
			{
				if (variable.key != null)
					form.AddField(variable.key, variable.value);
			}
		else
			form.AddField(" ", " ");
		return form;
	}


	public UnityWebRequest CreateWWWRequest(string url, ServerPostVariable[] variables, RequestType type)
	{
		WWWForm form = GetWWWForm(variables);

		UnityWebRequest www = UnityWebRequest.Post(url, form);
		//www = UnityWebRequest.Post()

		switch (type)
		{
			case RequestType.PUT:
				Debug.Log(form.data);
				www = UnityWebRequest.Put(url, form.data);
				break;
			case RequestType.DELETE:
				www = UnityWebRequest.Delete(url);
				break;
			case RequestType.GET:
				www = UnityWebRequest.Get(url);
				break;
		}
		www.SetRequestHeader("content-type", "application/x-www-form-urlencoded");
		return www;
	}


	/// <summary>
	/// Creates a WWW request from json.
	/// </summary>
	/// <returns>The WWWR equest from json.</returns>
	/// <param name="url">URL where to send to.</param>
	/// <param name="json">Json to send.</param>
	/// <param name="type">Type.</param>
	protected UnityWebRequest CreateWWWRequestFromJson(string url, string json, RequestType type)
	{
		var request = new UnityWebRequest(url, type.ToString());
		byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
		request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
		request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");

		return request;
	}



	public void ToApiWithInstallToken(string url, ApiCallBack callback, string key = null, string value = null, bool getReq = false)
	{
		SendToApi(url, callback, new ServerPostVariable(key, value));
	}


	public void PersistToApi(string url, ServerPostVariable variable = null, ApiController.RequestType type = ApiController.RequestType.POST)
	{
		PersistToApi(url, new ServerPostVariable[] { variable }, type);
	}


	public void PersistToApi(string url, ServerPostVariable[] variables = null, ApiController.RequestType type = ApiController.RequestType.POST)
	{
		UploadObject upload = new UploadObject(url, variables, type);
		uploads.Add(upload);
		SendToApi(url, RemoveFromPersist, variables, type);
	}

	public void RemoveFromPersist(string response, System.Net.HttpStatusCode responseCode)
	{
		if ((int)responseCode != 200 && (int)responseCode != 201)
		{
			return;
		}
		if (uploads.Count > 0)
			uploads.RemoveAt(0);
	}

	void OnApplicationQuit()
	{
		UploadSaveObject uploadSave = new UploadSaveObject(uploads.ToArray());
		//SecurePlayerPrefs.SetString(playerPrefsKey, JsonUtility.ToJson(uploadSave));
	}
}

class UploadObject
{
	public string url;
	public ServerPostVariable[] variables;
	public ApiController.RequestType requestType;

	public UploadObject(string url, ServerPostVariable[] variables, ApiController.RequestType requestType = ApiController.RequestType.POST)
	{
		this.url = url;
		this.variables = variables;
		this.requestType = requestType;
	}
}

class UploadSaveObject
{

	public UploadObject[] uploads;

	public UploadSaveObject(UploadObject[] uploads)
	{
		if (uploads == null)
		{
			this.uploads = new UploadObject[] { };
			return;
		}
		this.uploads = uploads;
	}
}

class ErrorResponse
{
	/// <summary>
	/// An unique slug identifieng this error
	/// </summary>
	[SerializeField]
	public string error { get; set; }
	/// <summary>
	/// Message for the developer with information what could have caused this error and how it could be fixed
	/// </summary>
	public string message { get; set; }
	/// <summary>
	/// Message for the user if apropiate helping him understand what he made wrong
	/// </summary>
	public string user_message;
	/// <summary>
	/// Additional documentation on this error
	/// </summary>
	public string info;

	public ErrorResponse FromJson(string json)
	{
		return JsonUtility.FromJson<ErrorResponse>(json);
	}
}



[System.Serializable]
public class ServerPostVariable
{
	public string key;
	public string value;

	public ServerPostVariable(string key, string value)
	{
		this.key = key;
		this.value = value;
	}

	public ServerPostVariable() { }
}
