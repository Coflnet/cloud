using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiImageSelectCaptcha : MonoBehaviour, ICaptchaChallenge
{
	private CaptchaImageToggler[] captchaImages;
	private CaptchaChallenge originalChallenge;
	private int loadedCount;


	public void DisplayChallenge(CaptchaChallenge challengeData, GameObject container)
	{
		loadedCount = 0;
		captchaImages = container.transform.GetComponentsInChildren<CaptchaImageToggler>();
		for (int i = 0; i < captchaImages.Length; i++)
		{
			CaptchaImageToggler toggler = captchaImages[i];

			// give the toggler a reference to this object to be able to execute our loadcallback
			toggler.SetCaptchaChallenge(this);


			CaptchaController.instance.LoadImage(challengeData.images[i], toggler.SetImage, "multiimage-select");

			// deselect tile
			captchaImages[i].Reset();
		}
		originalChallenge = challengeData;
	}

	public string GetChallengeData()
	{
		bool[] result = new bool[captchaImages.Length];
		for (int i = 0; i < captchaImages.Length; i++)
		{
			result[i] = captchaImages[i].GetResult();
		}

		MultiImageSelectSubmit submit = new MultiImageSelectSubmit(originalChallenge.images, result, originalChallenge.target, originalChallenge.token);

		return JsonUtility.ToJson(submit);
	}

	public string Slug => "multiimage-select";
	

	/// <summary>
	/// Called when Images were set.
	/// Will hide the loading animation when at least 15 Images are loaded
	/// </summary>
	public void LoadCallback()
	{
		loadedCount++;
		if (loadedCount >= 15)
		{
			CaptchaController.instance.HideLoadingAndShowChallange();
		}
	}
}


/// <summary>
/// Multi image select submit used for json serializing the challenge data
/// </summary>
[System.Serializable]
public class MultiImageSelectSubmit
{
	public string[] tokens;
	public bool[] result;
	public string target;
	public string target_token;



	public MultiImageSelectSubmit(string[] tokens, bool[] result, string target, string target_token)
	{
		this.tokens = tokens;
		this.result = result;
		this.target = target;
		this.target_token = target_token;
	}
}