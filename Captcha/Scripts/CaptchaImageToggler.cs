using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CaptchaImageToggler : MonoBehaviour
{
	private string token;
	private bool selected = false;

	private Image target;
	private RectTransform rect;

	ICaptchaChallenge challenge;


	public CaptchaImageToggler(ICaptchaChallenge challenge)
	{
		SetCaptchaChallenge(challenge);
	}

	public void SetCaptchaChallenge(ICaptchaChallenge challenge)
	{
		this.challenge = challenge;
	}

	void Setup()
	{
		target = this.transform.GetChild(0).GetComponent<Image>();
		rect = target.GetComponent<RectTransform>();
	}


	public void SetImage(Sprite image, string token)
	{
		if (rect == null)
			Setup();
		target.sprite = image;
		this.token = token;

		Button button = this.GetComponent<Button>();
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(Click);

		// set colors
		//Image tileImage = button.GetComponent<Image>();
		//tileImage.color = Color.blue;
		this.challenge.LoadCallback();
	}

	public void Click()
	{
		if (selected)
		{
			Reset();
		}
		else
		{
			rect.offsetMax = new Vector2(-10f, -10f);
			rect.offsetMin = new Vector2(10f, 10f);
		}
		selected = !selected;

		Debug.Log("clicked on captcha");
	}

	public void Reset()
	{
		selected = false;

		if (rect == null)
			return;
		//deselect
		rect.offsetMax = new Vector2(0f, 0f);
		rect.offsetMin = new Vector2(0f, 0f);
	}

	public bool GetResult()
	{
		return selected;
	}
}
