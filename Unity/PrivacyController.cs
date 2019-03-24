using System;
using System.Collections.Generic;
using UnityEngine;
using Coflnet;
using UnityEngine.UI;
using Coflnet.Unity;
using Coflnet.Client;

public class PrivacyController : MonoBehaviour, IPrivacyScreen
{


	public static int privacyLevel { get; private set; }
	private static int tmpPrivacyLevel = 5;
	public static PrivacyController instance;

	public Action<int> decissionCallback;

	public GameObject privacyScreen;
	public GameObject optionPrefab;
	public GameObject detailedSettingsConteiner;

	public GameObject moreInfoContainer;

	public PrivacySettings settings;


	void Awake()
	{
		instance = this;
		LoadSettings();
	}

	private void LoadSettings()
	{
		if (!PlayerPrefs.HasKey("privacySettings"))
		{

			return;
		}
		settings = JsonUtility.FromJson<PrivacySettings>(PlayerPrefs.GetString("privacySettings"));
	}

	// Use this for initialization
	void Start()
	{
		privacyLevel = 3;
		if (PlayerPrefs.HasKey("privacyLevel"))
			privacyLevel = PlayerPrefs.GetInt("privacyLevel");
		//else if (!GameController.firstStart)
		//{
		//DataController.Instance.a(ShowSelect);
		//}
		instance = this;


		//ShowPrivacyOptions();
	}


	public void DeleteMe()
	{
		NotificationHandler.instance.ShowLocalNotification(
		"confirm",
		"confirm_deletion",
		DeleteConfirmed
		);
	}

	public void DeleteConfirmed()
	{
		//string url = DataShifter.ufURL + "/api/v1/user";
		//ApiController.instance.SendToApi(url, DeletionCallback, ApiController.RequestType.DELETE);
	}

	public void DeletionCallback(string text, System.Net.HttpStatusCode responseCode)
	{
		if (responseCode == System.Net.HttpStatusCode.OK)
		{
			NotificationHandler.instance.AddLocalAlert("deletion_requested");
		}
	}



	public void SetPrivacyLevel(float level)
	{
		int levelInt = (int)Mathf.Round(level);
		Debug.Log(level);
		tmpPrivacyLevel = levelInt;
	}



	public void Accept()
	{
		PlayerPrefs.SetInt("privacyLevel", tmpPrivacyLevel);
		privacyLevel = tmpPrivacyLevel;

		NotificationHandler.instance.AddLocalAlert("privacy_updated");

		MenuController mc = MenuController.instance;
		mc.SetInActiveWithoutReverse(privacyScreen);

		decissionCallback?.Invoke(tmpPrivacyLevel);




		//mc.SetActiveWithoutReverse(mc.main);
		UnityAssetsLoader.Instance.LoadLocalFile(SetSettingsByLevel, "privacy_options.json");

		if (privacyLevel <= 1)
			ShowPrivacyOptions();

	}

	private void SetSettingsByLevel(string data)
	{
		PrivacyOption option = JsonUtility.FromJson<PrivacyOption>(data);
		SetSettingsByLevel(option);
	}

	private void SetSettingsByLevel(PrivacyOption option)
	{
		if (privacyLevel >= option.minimal_level)
		{
			// Doesn't use SetDecission to avaid to many saves
			settings.SetSetting(option.details, true);
		}
		else
		{
			settings.SetSetting(option.details, false);
		}
		if (option.sub_options != null && option.sub_options[0] != null)
		{
			for (int i = 0; i < option.sub_options.Length; i++)
			{
				SetSettingsByLevel(option.sub_options[i]);
			}
		}
		SaveSettings();
	}

	private void SaveSettings()
	{
		PlayerPrefs.SetString("privacySettings", JsonUtility.ToJson(settings));
	}


	public void ShowSelect()
	{
		MenuController.instance.SetActive(privacyScreen);
		// set previous decission
		Slider privacySelector = privacyScreen.transform.GetChild(1).GetChild(0).GetComponent<Slider>();
		Debug.Log("privacy level: " + privacyLevel);
		if (privacySelector != null)
		{
			privacySelector.value = (float)privacyLevel;
		}
		else
		{
			Debug.LogError("privacySelector not found");
		}
	}


	public void MoreDetails(string topic)
	{
		NotificationHandler.instance.AddMessageToAlertStream("More on " + topic + " soon :)");
	}


	public void ShowPrivacyOptions()
	{
		UnityAssetsLoader.Instance.LoadLocalFile(ShowPrivacyOptions, "privacy_options.json");
	}

	public void ShowPrivacyOptions(string data)
	{
		MenuController mc = MenuController.instance;
		mc.DeleteChilds(detailedSettingsConteiner);
		mc.SetActive(detailedSettingsConteiner.transform.parent.parent.parent.gameObject);
		mc.SetInActive(DummyGameObject.Instance);
		Debug.Log(data);
		PrivacyOption option = JsonUtility.FromJson<PrivacyOption>(data);
		GameObject fullAge = GenerateOption(option);
		fullAge.GetComponent<InstancePasser>().enableAll = false;
		fullAge.SetActive(true);

		if (AmIAllowedToDo("full-age"))
		{
			fullAge.GetComponent<InstancePasser>().ChangeSetting(true);
		}
	}



	public GameObject GenerateOption(PrivacyOption option, bool parentEnabled = false, int level = 0)
	{
		MenuController mc = MenuController.instance;
		// bind to correct setting
		GameObject optionObject = Instantiate(optionPrefab);
		Toggle toggle = optionObject.GetComponent<Toggle>();
		InstancePasser ip = optionObject.GetComponent<InstancePasser>();
		ip.identifier = option.details;
		toggle.onValueChanged.AddListener(ip.ChangeSetting);

		// display current settingsJson
		bool isEnabled = AmIAllowedToDo(option.details);
		Debug.Log(" I am allowed to do " + option.details + " " + isEnabled);
		toggle.isOn = isEnabled;
		if (parentEnabled)
			optionObject.SetActive(true);

		// translate text
		mc.SetChildText(optionObject, 0, option.translation_key);

		// add color to text to show that option may depend on antoher
		ChangeTextColor(optionObject, level);

		// ad "more" link
		Button button = mc.Child(optionObject, 0).GetComponent<Button>();
		string url = ConfigController.GetUrl("/privacy/" + option.details);
		button.onClick.AddListener(() =>
		{
			UnityEngine.Application.OpenURL(url);
		});

		// position in list
		optionObject.transform.SetParent(detailedSettingsConteiner.transform, false);


		// generate childs and asign them
		if (option.sub_options != null && option.sub_options[0] != null)
		{
			optionObject.transform.GetChild(2).gameObject.SetActive(true);

			ip.objects = new GameObject[option.sub_options.Length];
			for (int i = 0; i < option.sub_options.Length; i++)
			{
				PrivacyOption subOption = option.sub_options[i];

				ip.objects[i] = GenerateOption(subOption, isEnabled, level + 1);
			}
		}
		return optionObject;
	}

	private void ChangeTextColor(GameObject item, int level)
	{
		int colorLevel = 255 - level * 10;
		string hexValue = colorLevel.ToString("X");
		// RGB
		hexValue += hexValue + hexValue;
		Color theColor = new Color();
		ColorUtility.TryParseHtmlString('#' + hexValue, out theColor);
		MenuController.instance.SetChildTextColor(item, 0, theColor);
	}

	public void SetDecission(string identifier, bool decission)
	{
		Debug.Log(" Set " + identifier + " to " + decission);
		settings.SetSetting(identifier, decission);
		SaveSettings();
	}


	public bool AmIAllowedToDo(string identifier)
	{
		//CoflnetUser user;
		//if(!ReferenceManager.Instance.TryGetResource())
		return settings.IsSettingEnabled(identifier);
	}




	public void ShowMoreInfo(string translationKey)
	{
		MenuController menuController = MenuController.instance;
		LocalizationManager localizationManager = LocalizationManager.Instance;
		menuController.SetActive(moreInfoContainer);

		menuController.SetChildButtonText(moreInfoContainer, 0, translationKey);

		GameObject textContainer = menuController.Child(moreInfoContainer, 1);
		for (int i = 0; i < 4; i++)
		{
			menuController.SetChildText(textContainer, i,
										localizationManager.GetTranslation(translationKey + "_" + i));
		}
	}

	public void ShowScreen(Action<int> whenDone)
	{
		decissionCallback = whenDone;
	}
}


[System.Serializable]
public class PrivacyOptions
{
	public PrivacyOption[] options;
}

[System.Serializable]
public class PrivacyOption
{
	public string details;
	public string translation_key;
	public int minimal_level;
	public PrivacyOption[] sub_options;
}

[System.Serializable]
public class PrivacySettings
{
	public PrivacySetting[] settings;

	public void SetSetting(string identifier, bool enabled)
	{
		PrivacySetting setting = FindSetting(identifier);
		if (setting == null)
		{
			Array.Resize(ref settings, settings.Length + 1);
			settings[settings.Length - 1] = new PrivacySetting(identifier, enabled);
		}
		else
			setting.enabled = enabled;
	}

	public PrivacySetting FindSetting(string identifier)
	{
		for (int i = 0; i < settings.Length; i++)
		{
			if (settings[i].identifier == identifier)
				return settings[i];
		}
		return null;
	}

	public bool IsSettingEnabled(string identifier)
	{
		PrivacySetting setting = FindSetting(identifier);
		if (setting == null)
			return false;
		return setting.enabled;
	}
}

[System.Serializable]
public class PrivacySetting
{
	public string identifier;
	public bool enabled = false;

	public PrivacySetting(string identifier, bool enabled = false)
	{
		this.identifier = identifier;
		this.enabled = enabled;
	}
}