using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Coflnet;
using Coflnet.Client;
using Coflnet.Unity;


/// <summary>
/// Handles undoing user actions. 
/// Eg. user opening a menu and then clicking the return button on android.
/// </summary>
public class BackController : MonoBehaviour
{

	List<GameObject> active = new List<GameObject>();
	List<GameObject> inactive = new List<GameObject>();




	// up = 1, left = 2, down= 3, right= 4, 0 is none
	List<SwipeDirection> animated = new List<SwipeDirection>();

	Action backClick;

	public static int updatesSinceBackClick = 0;

	public static BackController instance;


	void Awake()
	{
		SetBackFunction(Reverse);
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}
	}



	public void WasSetInactive(GameObject ob)
	{
		inactive.Insert(0, ob);
	}

	/// <summary>
	/// Save that object has been set active to reverse it back
	/// </summary>
	/// <param name="ob">The object which has been set active</param>
	/// <param name="anim">Animation up = 1, left = 2, down= 3, right= 4, 0 is none</param>
	public void WasSetActive(GameObject ob, SwipeDirection anim = 0)
	{
		active.Insert(0, ob);
		animated.Insert(0, anim);
	}

	public void SetActive(GameObject ob, SwipeDirection anmin = 0)
	{
		ob.SetActive(true);
		WasSetActive(ob, anmin);
	}

	public void SetInActive(GameObject ob, int anmin = 0)
	{
		ob.SetActive(false);
		WasSetInactive(ob);
	}


	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Backspace))
		{
			BackClick();
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			BackClick();
		}
	}


	public void BackClick()
	{
		backClick();
		updatesSinceBackClick = 0;
	}


	public void BackClick(int i)
	{
		for (; i > 0; i--)
		{
			BackClick();
		}
	}

	/// <summary>
	/// Reverse the last action
	/// </summary>
	public void Reverse()
	{
		Debug.Log(active.Count.ToString() + inactive.Count.ToString());
		if (active.Count > 0)
		{
			//	if (animated[0] > 0)
			//		MenuController.instance.SwipeOut(active[0], animated[0]);
			//	else
			active[0].SetActive(false);

			animated.RemoveAt(0);
			active.RemoveAt(0);
		}
		else
			LeaveGame();

		if (inactive.Count > 0)
		{
			inactive[0].SetActive(true);
			inactive.RemoveAt(0);
			//SoundController.instance.NormalButtonClick();
		}
	}


	void LeaveGame()
	{
		string confirm = LocalizationManager.Instance.GetTranslation("confirm");
		string realyLeave = LocalizationManager.Instance.GetTranslation("realy_leave");
		NotificationHandler.instance.ShowNotification(confirm, realyLeave, UnityEngine.Application.Quit);
	}



	// Used to assign a function to the return button
	public void SetBackFunction(Action onClick)
	{
		backClick = onClick;
	}

	public void SetDefaultBackFunction()
	{
		backClick = Reverse;
	}


	void FixedUpdate()
	{
		updatesSinceBackClick++;
	}
}
