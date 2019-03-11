using System.Collections;
using System;
using UnityEngine;

public class UnityAssetsLoader : MonoBehaviour
{

	public static UnityAssetsLoader Instance;

	// Use this for initialization
	void Awake()
	{
		Instance = this;
	}



	public void LoadLocalFile(Action<string> callback, string name)
	{
		string localPath = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, name);
		if (localPath.Contains("://"))
		{
			StartCoroutine(LoadLocalFileAsync(callback, localPath));
		}
		else
			callback(System.IO.File.ReadAllText(localPath));

	}

	static IEnumerator LoadLocalFileAsync(Action<string> callback, string localPath)
	{
		WWW www = new WWW(localPath);
		yield return www;
		callback(www.text);
	}
}
