using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Coflnet;

public class LocalizedText : MonoBehaviour
{

	public string key;
	public bool prevent;

	// Use this for initialization
	void Start()
	{
		if (prevent)
			return;
		if (LocalizationManager.Instance.loadDone)
			SetText();
		else
			LocalizationManager.Instance.AddLoadCallback(Loaded);
	}

	public void Loaded()
	{
		SetText();
	}


	public void SetText()
	{
		Text text = GetComponent<Text>();
		text.text = LocalizationManager.Instance.GetTranslation(key);
	}

#if UNITY_WEBGL
	void OnEnable(){
		SetText ();
	}
#endif
}