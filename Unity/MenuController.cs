using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

namespace Coflnet.Unity
{

	public class MenuController : MonoBehaviour
	{

		public static MenuController instance;

		static MenuController()
		{
			instance = new MenuController();
		}


		public void SetActive(GameObject toActive)
		{
			toActive.SetActive(true);
			BackController.instance.WasSetActive(toActive);
		}


		/// <summary>
		/// Combines Set Active and setinactive
		/// </summary>
		/// <param name="toActive">To active.</param>
		/// <param name="toInactive">To inactive.</param>
		public void SetActive(GameObject toActive, GameObject toInactive, bool silent = false)
		{
			SetActive(toActive);
			SetInActive(toInactive, silent);
		}

		public void SetActiveDummy(GameObject toActive, bool silent = false)
		{
			SetActive(toActive);
			SetInActive(DummyGameObject.Instance, silent);
		}





		public void SetInActive(GameObject toInactive, bool silent = false)
		{
			toInactive.SetActive(false);
			BackController.instance.WasSetInactive(toInactive);
			if (!silent)
				SoundController.instance.NormalButtonClick();
		}



		public void SetInActive(GameObject toInactive)
		{
			SetInActive(toInactive, false);
		}



		public void SetInActiveAfterSwipe(GameObject toInactive)
		{
			//StartCoroutine(DisableAfterSeconds(toInactive, 0.5f));
			BackController.instance.WasSetInactive(toInactive);
			SoundController.instance.NormalButtonClick();
		}



		public void SetActiveWithSwipeLeft(GameObject toSwipe)
		{
			// up = 1, left = 2, down= 3, right= 4, 0 is none
			// the directions to swipe out to
			SwipeIn(toSwipe, SwipeDirection.LEFT);
		}

		public void SwipeIn(GameObject toSwipe, SwipeDirection direction = SwipeDirection.NONE, float time = 0.5f)
		{
			BackController.instance.SetActive(toSwipe, direction);
			Vector2 endPos = new Vector2();
			//endPos.x = Screen.width / 2;
			//endPos.y = Screen.height / 2;
			endPos.x = 0f;
			endPos.y = 1f;
			iTween.MoveTo(toSwipe, endPos, time);
		}


		public void SwipeOut(GameObject toSwipe, SwipeDirection direction, float time = 0.5f)
		{
			Vector2 endPos = new Vector2();
			// up = 1, left = 2, down= 3, right= 4, 0 is none
			switch (direction)
			{
				case SwipeDirection.RIGHT:
					//endPos.x = Screen.width * 1.5f;
					//endPos.y = Screen.height / 2;
					endPos.x = 1.5f;
					endPos.y = 1;
					break;
				case SwipeDirection.LEFT:
					endPos.x = Screen.width * -0.5f;
					endPos.y = Screen.height / 2;
					break;
				case SwipeDirection.DOWN:
					endPos.x = Screen.width / 2;
					endPos.y = Screen.height * -0.5f;
					break;
				case SwipeDirection.UP:
					endPos.x = Screen.width / 2;
					endPos.y = Screen.height * 1.5f;
					break;
				default:
					break;
			}
			iTween.MoveTo(toSwipe, endPos, time);
			// causes more performance issues than it solves
			//IEnumerator disable = DisableAfterSeconds(toSwipe, time);         
			//StartCoroutine(disable);
		}




		public void SetActiveWithoutReverse(GameObject toActive)
		{
			toActive.SetActive(true);
		}



		public void SetInActiveWithoutReverse(GameObject toInactive)
		{
			toInactive.SetActive(false);
			SoundController.instance.NormalButtonClick();
		}

		/// <summary>
		/// Deletes all Child objects.
		/// </summary>
		/// <param name="parent">Parent.</param>
		public void DeleteChilds(GameObject parent)
		{
			foreach (Transform child in parent.transform)
			{
				Destroy(child.gameObject);
			}
		}

		public void DeactivateCanvasChilds(int execlude = 100000)
		{
			//Deactivate everything that could overlay the game
			GameObject canvas = GameObject.Find("Canvas");
			for (int i = 0; i < canvas.transform.childCount; i++)
			{
				if (i != execlude)
					MenuController.instance.Child(canvas, i).SetActive(false);
			}
		}

		public void DeactivateCanvasChilds(String[] execlude)
		{
			GameObject canvas = GameObject.Find("Canvas");
			foreach (Transform item in canvas.transform)
			{
				if (!execlude.Contains<string>(item.gameObject.name))
					item.gameObject.SetActive(false);
			}
		}



		/// <summary>
		/// Get a childobject from index
		/// </summary>
		/// <param name="parent">Parent.</param>
		/// <param name="index">Index.</param>
		public GameObject Child(GameObject parent, int index = 0)
		{
			return Child(parent.transform, index);
		}


		public GameObject Child(Transform parent, int index = 0)
		{
			return parent.GetChild(index).gameObject;
		}


		/// <summary>
		/// Sets a given Text to the text of an objects child
		/// </summary>
		/// <returns>The child text.</returns>
		/// <param name="parent">Parent.</param>
		/// <param name="index">Index.</param>
		/// <param name="text">Text.</param>
		public void SetChildText(GameObject parent, int index, string text)
		{

			Text textObject = Child(parent, index).GetComponent<Text>();
			if (text == "")
			{
				textObject.text = "";
				Child(parent, index).SetActive(false);
			}

			else
			{
				// localize Text
				text = LocalizationManager.Instance.GetTranslation(text);
				ShowOffEmoji.instance.SetText(textObject, text);
				Child(parent, index).SetActive(true);
			}
		}

		public void SetChildTextColor(GameObject parent, int index, Color color)
		{
			Text textObject = Child(parent, index).GetComponent<Text>();
			textObject.color = color;
		}


		public void SetChildText(Transform parent, int index, string text)
		{
			SetChildText(parent.gameObject, index, text);
		}

		public void SetChildButtonText(GameObject parent, int index, string text)
		{
			SetChildText(Child(parent, index), 0, text);
		}


		public void SetChildsInactive(GameObject parent)
		{
			// Deaktivate all child objects, to show the right screen later.
			DisableAllChilds(parent);
		}


		public void SetTranslatedChildText(GameObject parent, int index, string transKey)
		{
			SetChildText(parent, index, LocalizationManager.Instance.GetTranslation(transKey));
		}

		public void SetTranslatedChildButtonText(GameObject parent, int index, string transKey, params KeyValuePair<string, string>[] values)
		{
			SetChildButtonText(parent, index, LocalizationManager.Instance.GetTranslation(transKey, values));
		}

		public void ShowThanksTo()
		{
			string text = LocalizationManager.Instance.GetTranslation("thanks_to");
			NotificationHandler.instance.ShowNotification("", text);
		}


		public void DisplayNotUnderFifty()
		{
			string text = LocalizationManager.Instance.GetTranslation("not_under_fifty");
			string title = LocalizationManager.Instance.GetTranslation("not_yet_unlocked");
			NotificationHandler.instance.ShowNotification(title, text);
		}




		public void SetChildImage(Transform parent, int index, Sprite sprite)
		{
			Child(parent, index).GetComponent<Image>().sprite = sprite;
		}


		public Button GetChildButton(GameObject parent, int index)
		{
			return Child(parent, index).GetComponent<Button>();
		}

		public void AddChildButtonAction(GameObject parent, int index, UnityAction action)
		{
			GetChildButton(parent, index).onClick.AddListener(action);
		}

		public void SetChildButtonAction(GameObject parent, int index, UnityAction action)
		{
			Button target = GetChildButton(parent, index);
			target.onClick.RemoveAllListeners();
			target.onClick.AddListener(action);
		}

		public void EnableAllChilds(GameObject parent)
		{
			ChangeChildStates(parent, true);
		}

		public void DisableAllChilds(GameObject parent)
		{
			ChangeChildStates(parent, false);

		}

		public void ChangeChildStates(GameObject parent, bool state)
		{
			foreach (Transform child in parent.transform)
			{
				child.gameObject.SetActive(state);
			}
		}

		public void ChangeObjectState(GameObject[] objects, bool state)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].SetActive(state);
			}
		}
	}

}