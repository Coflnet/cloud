using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Coflnet;
using Coflnet.Client;

public class CaptchaController : MonoBehaviour
{
	void HandleUnityAction()
	{
	}


	public GameObject captchaPrefab;

	private GameObject captchaObject;


	private ICaptchaChallenge currentCaptcha;
	private string passToken = "";

	public static readonly string apiUrl = "https://beta.coflnet.com/api/v1/";

	public delegate void SpriteCallBack(Sprite sprite, string token);
	public delegate void TokenCallback(string token);

	private TokenCallback passTokenCallback;

	public static CaptchaController instance;

	private bool isActive;

	void Awake()
	{
		instance = this;
		captchaObject = this.gameObject;
	}

	/// <summary>
	/// Hides the captcha.
	/// </summary>
	public void HideCaptcha()
	{
		captchaObject.SetActive(false);
	}

	void Start()
	{
		RegisterErrorHandlers();
		//ShowCaptcha();
	}

	/// <summary>
	/// Returns the latest pass token if one is available.
	/// Remember pass_tokens can only be used once!
	/// </summary>
	/// <returns>The pass token.</returns>
	public string GetPassToken()
	{
		return passToken;
	}

	/// <summary>
	/// Registers the coflnet error handlers.
	/// </summary>
	protected void RegisterErrorHandlers()
	{
		ApiController.instance.RegisterErrorHandler("wrong_label", ErrorHandler);
		ApiController.instance.RegisterErrorHandler("token_invalid", ErrorHandler);
		ApiController.instance.RegisterErrorHandler("token_timeout", ErrorHandler);
	}

	/// <summary>
	/// Handles errors returned by the API
	/// </summary>
	/// <param name="userMessage">User message.</param>
	public void ErrorHandler(string userMessage)
	{
		// There is not much we can do
		// only option is to start all over again 
		ShowCaptcha();
	}


	/// <summary>
	/// Shows the captcha.
	/// </summary>
	public void ShowCaptcha()
	{
		ShowCaptcha(null);
	}

	/// <summary>
	/// Sets up the animations.
	/// </summary>
	protected void SetupAnimations()
	{
		ResetSubmitAnimation();
		isActive = true;
		HideSuccess();
		ShowLoadingAnimation();
		GetChallangeAnimator().gameObject.SetActive(false);
	}


	/// <summary>
	/// Shows the captcha.
	/// </summary>
	/// <param name="callback">Callback.</param>
	public void ShowCaptcha(TokenCallback callback)
	{
		SetupAnimations();
		DisableControlButtons();

		if (callback != null)
			passTokenCallback = callback;

		LoadNewCaptcha();

		if (captchaObject == null)
			InstantiateCaptcha();
	}

	protected void LoadNewCaptcha()
	{
		string url = apiUrl + "captcha";
		ApiController.instance.SendToApi(url, ReceiveApiResponse, ApiController.RequestType.GET);
	}

	/// <summary>
	/// Will be called if no GameObject has been found in scene
	/// </summary>
	protected void InstantiateCaptcha()
	{
		GameObject suspectedCanvas = GameObject.Find("Canvas");
		if (suspectedCanvas == null)
		{
			throw new System.Exception("No canvas found in your scene");
		}
		Canvas canvas = suspectedCanvas.GetComponent<Canvas>();
		captchaObject = Instantiate(captchaObject);
		captchaObject.transform.SetParent(canvas.transform, false);
		captchaObject.SetActive(true);
	}

	/// <summary>
	/// Receives the challenge and tries to display it
	/// </summary>
	/// <param name="data">Data.</param>
	/// <param name="responseCode">Response code.</param>
	public void ReceiveApiResponse(string data, System.Net.HttpStatusCode responseCode)
	{
		Debug.Log("received captcha");
		CaptchaChallengeResponse response = JsonUtility.FromJson<CaptchaChallengeResponse>(data);
		// it is possible that we allready have a pass token
		if (response.pass_token != null)
		{
			ReceivePassToken(response.pass_token);
		}
		else
			switch (response.slug)
			{
				case "multiimage-select":
					DisplayMultiImageSelect(response.challenge);
					break;
				default:
					Debug.LogError("API responded with unknown captcha type");
					break;
			}

	}


	/// <summary>
	/// Receives the pass token and invokes the callback if given
	/// </summary>
	/// <param name="passToken">Pass token which vertifies this user as human.</param>
	protected void ReceivePassToken(string passToken)
	{
		ShowSuccess();
		Debug.Log("Succeeded: " + passToken);
		if (passTokenCallback != null)
		{
			passTokenCallback(passToken);
			isActive = false;
			StartCoroutine(DelayedHide());
		}
	}


	IEnumerator DelayedHide()
	{
		yield return new WaitForSeconds(1);
		if (!isActive)
			HideCaptcha();
	}

	private void EnableControlButtons()
	{
		Transform container = captchaObject.transform.GetChild(0).GetChild(2);
		SetChildButtonAction(container, Submit, 1, true);
		SetChildButtonAction(container, ShowCaptcha, 0, true);
	}

	protected void DisableControlButtons()
	{
		Transform container = captchaObject.transform.GetChild(0).GetChild(2);
		DisableChildButton(container, 1);
		DisableChildButton(container);
	}

	protected void DisableChildButton(Transform parent, int index = 0)
	{
		parent.GetChild(index).GetComponent<Button>().interactable = false;
	}


	protected void DisplayMultiImageSelect(CaptchaChallenge challenge)
	{
		currentCaptcha = new MultiImageSelectCaptcha();
		Transform captchaScreen = captchaObject.transform.GetChild(0);
		currentCaptcha.DisplayChallenge(challenge, GetChallangeAnimator().gameObject);

		string translation = Coflnet.LocalizationManager.Instance.GetTranslation("please_select_all_x", new KeyValuePair<string, string>("target", challenge.target));

		captchaScreen.GetChild(0).GetChild(0).GetComponent<Text>().text = translation;

		Debug.Log("showed captcha");
	}


	private void ShowLoadingAnimation()
	{
		GetLoadingAnimationObject().SetActive(true);
		GetLoadingAnimationObject().GetComponent<Animator>().SetBool("hide", false);
	}

	public void HideLoadingAnimation(bool instant = false)
	{
		int time = 1;
		if (instant)
		{
			time = 0;
		}
		else
		{
			GetLoadingAnimationObject().GetComponent<Animator>().SetBool("hide", true);
		}
		StartCoroutine(DelayFunction(() =>
		{
			GetLoadingAnimationObject().SetActive(false);
		}, time));
	}

	public void HideLoadingAndShowChallange()
	{
		HideLoadingAnimation();
		GameObject challenge = GetChallangeAnimator().gameObject;
		// the animation sometimes doesn't get triggered properly
		challenge.SetActive(false);
		challenge.SetActive(true);

		EnableControlButtons();
	}

	protected GameObject GetLoadingAnimationObject()
	{
		return GetContainerTransform().GetChild(4).gameObject;
	}

	protected GameObject GetSuccessAnimationObject()
	{
		return GetContainerTransform().GetChild(3).gameObject;
	}

	/// <summary>
	/// Returns the visable captcha container 
	/// </summary>
	/// <returns>The container transform.</returns>
	protected Transform GetContainerTransform()
	{
		return captchaObject.transform.GetChild(0);
	}

	/// <summary>
	/// Displaies the animation check mark
	/// </summary>
	private void ShowSuccess()
	{
		HideLoadingAnimation(true);
		GetSuccessAnimationObject().SetActive(true);
	}

	/// <summary>
	/// Hides the success animation check mark
	/// </summary>
	private void HideSuccess()
	{
		GetSuccessAnimationObject().SetActive(false);
	}


	/// <summary>
	/// Downloads and saves an image, can load something into a sprite
	/// </summary>
	/// <param name="token">Token for this image.</param>
	/// <param name="callback">Callback to execute on success.</param>
	/// <param name="challengeType">Challenge type.</param>
	public void LoadImage(string token, SpriteCallBack callback, string challengeType)
	{
		StartCoroutine(DownloadImage(token, callback, challengeType));
	}

	/// <summary>
	/// Downloads and saves an image, can load something into a sprite (does the actual loading)
	/// </summary>
	/// <returns>An image.</returns>
	/// <param name="token">Token for this image</param>
	/// <param name="challengeType">Type of the challenge</param>
	IEnumerator DownloadImage(string token, SpriteCallBack callback, string challengeType)
	{
		string url = apiUrl + "captcha/c/" + challengeType + "/data/" + token;

		Texture2D textureImage = new Texture2D(120, 120);

		if (callback == null)
		{
			throw new System.Exception("You have to specify what function will handle the response");
		}
		WWW www = new WWW(url);
		yield return www;
		if (string.IsNullOrEmpty(www.error))
		{
			www.LoadImageIntoTexture(textureImage);
			textureToSprite(textureImage, callback, token);
		}
		else
		{
			Debug.Log("error response: " + www.text);
			// looks like this is an not existing image, tell the user
			string img_not_found = Coflnet.LocalizationManager.Instance.GetTranslation("img_not_found");
			NotificationHandler.instance.AddMessageToAlertStream(img_not_found);
		}
	}

	/// <summary>
	/// Converts a Texture2D into a sprite
	/// </summary>
	/// <param name="textureImage">Texture image.</param>
	/// <param name="callback">Callback.</param>
	/// <param name="token">Token for this image.</param>
	public void textureToSprite(Texture2D textureImage, SpriteCallBack callback, string token)
	{
		// Convert the image to a sprite
		Rect rect = new Rect(0, 0, textureImage.width, textureImage.height);
		//textureImage = TextureFilter.GaussianKernel(1,3);
		Sprite sprite = Sprite.Create(textureImage, rect, new Vector2(0, 0), 100f);


		// Overgive the sprite to the callback
		callback(sprite, token);
	}


	/// <summary>
	/// Invoked when the users clicks Submit
	/// Collects the challenge data and sends it to the API 
	/// </summary>
	public void Submit()
	{
		DisableControlButtons();
		PlaySubmitAnimation();
		ShowLoadingAnimation();

		string data = currentCaptcha.GetChallengeData();
		string url = apiUrl + "captcha/c/" + currentCaptcha.Slug;
		ApiController.instance.SendToApi(url, ReceiveApiResponse, data);
	}

	/// <summary>
	/// Plays the submit animation.
	/// </summary>
	public void PlaySubmitAnimation()
	{
		SetSubmitState(true);
	}

	/// <summary>
	/// Returns the challange Container animator.
	/// Available animations are Entrance (played automatically) and "submit".
	/// </summary>
	/// <returns>The challange animator.</returns>
	protected Animator GetChallangeAnimator()
	{
		return GetContainerTransform().GetChild(1).GetComponent<Animator>();
	}

	/// <summary>
	/// Resets the submit animation.
	/// </summary>
	public void ResetSubmitAnimation()
	{
		SetSubmitState(false);
	}

	/// <summary>
	/// Sets the state of the submit animation
	/// </summary>
	/// <param name="state">If set to <c>true</c> state.</param>
	protected void SetSubmitState(bool state)
	{
		Animator challangeAnimator = GetChallangeAnimator();
		challangeAnimator.SetBool("submit", state);
	}

	/// <summary>
	/// Sets an UnityAction on a child button
	/// </summary>
	/// <param name="parent">Parent under which to search for the button.</param>
	/// <param name="action">The action which to set the button to.</param>
	/// <param name="index">The index of the child under the parent that is the button</param>
	/// <param name="activateAndEnable">If set to <c>true</c> button will be activated and enabled.</param>
	private void SetChildButtonAction(Transform parent, UnityEngine.Events.UnityAction action, int index = 0, bool activateAndEnable = false)
	{
		Button button = parent.GetChild(index).GetComponent<Button>();
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(action);
		if (activateAndEnable)
		{
			button.interactable = true;
			button.gameObject.SetActive(true);
		}
	}

	/// <summary>
	/// Delaies the function.
	/// </summary>
	/// <returns>The function.</returns>
	/// <param name="action">Action.</param>
	/// <param name="amount">Amount.</param>
	IEnumerator DelayFunction(UnityAction action, float amount)
	{
		yield return new WaitForSeconds(amount);
		action();
	}
}


[System.Serializable]
public class CaptchaChallengeResponse
{
	public string slug;
	public CaptchaChallenge challenge;
	public string pass_token;
}

[System.Serializable]
public class CaptchaChallenge
{
	// for multiimage-select
	/// <summary>
	/// Array of image tokens
	/// </summary>
	public string[] images;
	/// <summary>
	/// Human readable word to tell the user what to look for
	/// </summary>
	public string target;
	/// <summary>
	/// Is a token to validate the target
	/// </summary>
	public string token;
}
