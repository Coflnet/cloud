using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using Coflnet;
using Coflnet.Client;
using Coflnet.Unity;

public class NotificationHandler : MonoBehaviour, INotificationDisplay
{
	public Animator animator;
	public GameObject alertObject;
	public GameObject notificationObject;



	MenuController mc;

	bool showing;


	int timesToUpdateBeforeEnableClose = 40;
	int timesUpdateRan = 50;

	private string firebaseToken;

	List<NotificationItem> asyncNotifications = new List<NotificationItem>();
	List<AlertItem> alerts = new List<AlertItem>();

	public static NotificationHandler instance;

	/*	public static NotificationHandler get(){

			if (instance == null) {
				instance = this;
			}

			return instance;
		} */

	void Awake()
	{
		instance = this;
	}


	void Start()
	{
		mc = MenuController.instance;
		LocalizationManager.Instance.AddLoadCallback(AfterLoad);

		Debug.Log("starting notificationhandler for firebase " + PrivacyController.instance.AmIAllowedToDo("share-firebase"));
		if (!PrivacyController.instance.AmIAllowedToDo("share-firebase"))
		{
			Debug.Log("firebase is disabled");
			return;
		}
		else
		{

			Debug.Log("enabled firebase");
		}
		Firebase.Messaging.FirebaseMessaging.TokenRegistrationOnInitEnabled = true;


		Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
		Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
	}


	void AfterLoad()
	{
		string welcome = LocalizationManager.Instance.GetTranslation("welcome_to_game");
		AddMessageToAlertStream(welcome);
	}

	void FixedUpdate()
	{
		if (!showing && alerts.Count > 0)
			ShowNextAlert();
	}

	void Update()
	{
		/*//TODO: fix the bug, that notifiation after another is disabled
		timesUpdateRan++;
		if (timesUpdateRan <= timesToUpdateBeforeEnableClose)
			notificationObjectifaction.SetActive (true);*/
	}

	public void AddMessageToAlertStream(string text)
	{
		alerts.Add(new AlertItem(text));
		if (!showing)
			ShowNextAlert();
	}


	public void AddMessageToStreamAsync(string text)
	{
		alerts.Add(new AlertItem(text));
	}

	public void ShowNextAlert()
	{
		showing = true;
		StartCoroutine(DisplayNextAlert());

	}


	public void AddActionAlert(string text, Action action, string actionText = "OK")
	{
		alerts.Add(new AlertItem(text, action, actionText));
		if (!showing)
			ShowNextAlert();
	}


	IEnumerator DisplayNextAlert()
	{
		alertObject.SetActive(true);

		Button actionButton = mc.Child(alertObject, 1).GetComponent<Button>();

		if (alerts[0].action != null)
		{
			actionButton.gameObject.SetActive(true);
			actionButton.onClick.RemoveAllListeners();
			actionButton.onClick.AddListener(() =>
			{
				alerts[0].action.Invoke();
			});
			mc.SetChildButtonText(alertObject, 1, alerts[0].buttonText);
			// the actual notification
			mc.SetChildText(alertObject, 0, alerts[0].text);
			mc.SetChildText(alertObject, 2, "");
		}
		else
		{
			actionButton.gameObject.SetActive(false);
			mc.SetChildText(alertObject, 2, alerts[0].text);
			mc.SetChildText(alertObject, 0, "");
		}

		animator.SetBool("ShowAlert", true);
		yield return new WaitForSeconds(2.5f);
		animator.SetBool("ShowAlert", false);
		//alertObject.SetActive (false);

		yield return new WaitForSeconds(2f);
		if (alerts.Count > 0)
			alerts.RemoveAt(0);
		if (alerts.Count > 0)
			ShowNextAlert();

		showing = false;
	}



	public void AddLocalAlertForFriend(string key, SourceReference friendId)
	{
		CoflnetUser data;
		if (ReferenceManager.Instance.TryGetResource<CoflnetUser>(friendId, out data))
		{
			AddLocalAlert(key, new KeyValuePair<string, string>("name", data.UserName));
		}
		else
		{
			//string name = PlayerController.instance.GetNameOfPlayer(friendId);
			AddLocalAlert(key, new KeyValuePair<string, string>("name", friendId.ToString()));
		}
	}


	public void AddLocalAlert(string key, params KeyValuePair<string, string>[] values)
	{
		string translationWithExtra = LocalizationManager.Instance.GetTranslation(key, values);
		AddMessageToAlertStream(translationWithExtra);
	}


	public void AddLocalAlertAsync(string key, params KeyValuePair<string, string>[] values)
	{
		string translationWithExtra = LocalizationManager.Instance.GetTranslation(key, values);
		AddMessageToStreamAsync(translationWithExtra);
		translationWithExtra = null;
	}


	public void ShowLocalNotification(
		string title,
		string text,
		string buttonText = "OK")
	{
		LocalizationManager lm = LocalizationManager.Instance;
		ShowNotification(lm.GetTranslation(title), lm.GetTranslation(text), lm.GetTranslation(buttonText));
	}

	/// <summary>
	/// Shows a notification.
	/// </summary>
	/// <param name="title">Title.</param>
	/// <param name="text">Text.</param>
	/// <param name="buttonText">Button text.</param>
	public void ShowNotification(string title,
		string text,
		string buttonText = "OK")
	{
		// Get the Child element of the plane
		GameObject actualNotification = mc.Child(notificationObject, 0);
		mc.SetChildText(actualNotification, 0, title);
		mc.SetChildText(actualNotification, 1, text);

		// Get the button Container
		mc.SetChildButtonText(actualNotification, 3, buttonText);

		// Disable the second button
		mc.Child(actualNotification, 2).SetActive(false);

		mc.Child(actualNotification, 3).SetActive(true);

		// Show the notification
		EnableGameObject();
		timesUpdateRan = 0;
		// Add
	}

	public void ShowNotificationAsync(
		string title,
		string text,
		UnityAction buttonRight = null,
		string buttonTextLeft = "no",
		string buttonTextRight = "yes",
		Action buttonLeft = null)
	{
		NotificationItem item = new NotificationItem();
		item.title = title;
		item.text = text;
		item.buttonRight = buttonRight;
		asyncNotifications.Add(item);

		ThreadController.Instance.OnMainThread(ShowNotification);
	}

	void ShowNotification()
	{
		NotificationItem item = asyncNotifications[0];
		if (item.buttonRight == null)
		{
			ShowNotification(item.title, item.text);
		}
		else
		{
			ShowNotification(
				item.title,
				item.text,
				item.buttonRight,
				item.buttonTextLeft,
				item.buttonTextRight);
		}
		asyncNotifications.RemoveAt(0);
	}

	/// <summary>
	/// Shows localized notification.
	/// </summary>
	/// <param name="title">Title localizationKey</param>
	/// <param name="text">Text localizationKey</param>
	/// <param name="buttonRight">Button action right.</param>
	/// <param name="buttonTextLeft">Button text left  localizationKey</param>
	/// <param name="buttonTextRight">Button text right localizationKey</param>
	/// <param name="buttonLeft">Button Action left.</param>
	public void ShowLocalNotification(
		string title,
		string text,
		UnityAction buttonRight,
		string buttonTextLeft = "no",
		string buttonTextRight = "yes",
		UnityAction buttonLeft = null)
	{
		LocalizationManager lm = LocalizationManager.Instance;
		ShowNotification(
			lm.GetTranslation(title),
			lm.GetTranslation(text),
			buttonRight,
			lm.GetTranslation(buttonTextLeft),
			lm.GetTranslation(buttonTextRight),
			buttonLeft
		);
	}

	/// <summary>
	/// Shows a notification.
	/// </summary>
	/// <returns>The notification.</returns>
	/// <param name="title">Title.</param>
	/// <param name="text">Text.</param>
	/// <param name="buttonRight">Button right.</param>
	/// <param name="buttonTextLeft">Button text left.</param>
	/// <param name="buttonTextRight">Button text right.</param>
	/// <param name="buttonLeft">Button left.</param>
	public void ShowNotification(
		string title,
		string text,
		UnityAction buttonRight,
		string buttonTextLeft = "no",
		string buttonTextRight = "yes",
		UnityAction buttonLeft = null)
	{
		// Get the Child element of the plane
		GameObject actualNotification = mc.Child(notificationObject, 0);

		// TODO make a system that translates title and text here
		// remember to implement the variables

		LocalizationManager local = LocalizationManager.Instance;
		if (buttonTextLeft == "no")
			buttonTextLeft = local.GetTranslation("no");
		if (buttonTextRight == "yes")
			buttonTextRight = local.GetTranslation("yes");


		// Set the texts
		mc.SetChildText(actualNotification, 0, title);
		mc.SetChildText(actualNotification, 1, text);
		GameObject buttonContainer = mc.Child(actualNotification, 2);

		// Hide the OK button
		mc.Child(actualNotification, 3).SetActive(false);
		// Set the container active
		buttonContainer.SetActive(true);


		mc.SetChildButtonText(buttonContainer, 0, buttonTextLeft);
		mc.SetChildButtonText(buttonContainer, 1, buttonTextRight);

		//Set the button actions
		Button left = mc.Child(buttonContainer, 0).GetComponent<Button>();
		Button right = mc.Child(buttonContainer, 1).GetComponent<Button>();

		left.onClick.RemoveAllListeners();
		right.onClick.RemoveAllListeners();


		// the BackClick needs to prevent hiding when a new notification is triggered in the callback                                                                                
		left.onClick.AddListener(BackController.instance.BackClick);
		right.onClick.AddListener(BackController.instance.BackClick);




		right.onClick.AddListener(buttonRight);
		if (buttonLeft != null)
			left.onClick.AddListener(buttonLeft);

		EnableGameObject(true);
		timesUpdateRan = 0;
	}


	void EnableGameObject(bool silent = false)
	{
		mc.SetActiveWithSwipeLeft(notificationObject);
		mc.SetInActive(DummyGameObject.Instance, silent);
	}






	public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
	{
		if (!PrivacyController.instance.AmIAllowedToDo("share-firebase"))
		{
			return;
		}
		firebaseToken = token.Token;

		Track.instance.SendTrackingRequest("Received firebase token/");
		// if the token is allready sent stop here;
		if (PlayerPrefs.GetString("firebaseToken", "noToken") == firebaseToken)
			return;
		/*string url = DataShifter.ufURL + "/api/v1/user/token/push";
		ServerPostVariable[] fields = {
			new ServerPostVariable("token",token.Token),
			new ServerPostVariable("provider","firebase")
		}; */
		//MultiplayerController.instance.SendToApi(url, TokenCallback, fields);
	}

	public void TokenCallback(string response, System.Net.HttpStatusCode status)
	{
		if (status != System.Net.HttpStatusCode.OK)
			return;
		PlayerPrefs.SetString("firebaseToken", firebaseToken);
	}

	public void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
	{
		UnityEngine.Debug.Log("Received a new message data: " + e.Message.Data);
		Track.instance.SendTrackingRequest("got message");
		foreach (System.Collections.Generic.KeyValuePair<string, string> iter in
			e.Message.Data)
		{
			Track.instance.SendTrackingRequest("Received a new message data: " + iter.Key + " -> " + iter.Value);
		}
	}

	public bool TryShowNextAlert(AlertItem alertItem)
	{
		alerts.Add(alertItem);
		ShowNextAlert();
		return true;
	}
}



[System.Serializable]
public class NotificationItem
{
	public string title;
	public string text;
	public UnityAction buttonRight;
	public string buttonTextLeft = "no";
	public string buttonTextRight = "yes";
	public UnityAction buttonLeft = null;
}
