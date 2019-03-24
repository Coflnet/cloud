using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{

	public static GameObject soundObject;
	public static SoundController instance;

	public AudioClip levelCompleted;

	public AudioClip normalButton;

	public AudioClip money;

	public AudioClip keyBoardKey;
	public AudioClip wrongAnswer;
	public AudioClip removeLetter;

	public AudioClip pop1;
	public AudioClip pop2;
	public AudioClip pop3;
	public AudioClip pop4;
	public AudioClip pop5;
	public AudioClip pop6;
	public AudioClip pop7;
	public AudioClip pop8;

	public AudioClip message;

	AudioClip currentLevelClip;

	public AudioSource audio;


	bool isSilent = false;

	void Awake()
	{
		instance = this;
	}

	public static bool playRemovLetter = false;

	void Start()
	{
		audio = GetComponent<AudioSource>();
	}



	public void SetSilent(bool silent)
	{
		isSilent = silent;
	}

	public void LevelCompleted()
	{
		PlaySound(levelCompleted);
	}


	public void NormalButtonClick()
	{
		this.gameObject.SetActive(true);
		PlaySound(normalButton);
	}

	public void LoadLevelButoon()
	{
		PlaySound(normalButton);
	}


	public void Money()
	{
		PlaySound(money);
	}

	public void KeyBoardKey()
	{
		PlaySound(keyBoardKey);
	}

	public static void RemoveLetterStatic()
	{
		playRemovLetter = true;
	}

	public void RemoveLetter()
	{
		playRemovLetter = false;
		PlaySound(removeLetter, 0.4F);
	}

	public void WrongAnswer()
	{
		PlaySound(wrongAnswer);
	}

	void Update()
	{
		if (playRemovLetter)
			RemoveLetter();
	}

	public void NewMsgSound()
	{
		PlaySound(message);
	}


	public void PlayStart()
	{
		StartCoroutine(StartingSounds());
	}


	IEnumerator StartingSounds()
	{
		float time = 0.08f;
		yield return new WaitForSeconds(0.5f);
		PlaySound(pop1);
		yield return new WaitForSeconds(time);
		PlaySound(pop1);
		yield return new WaitForSeconds(time);
		PlaySound(pop2);
		yield return new WaitForSeconds(time);
		PlaySound(pop2);
		yield return new WaitForSeconds(time);
		PlaySound(pop3);
		yield return new WaitForSeconds(time);
		PlaySound(pop3);
		yield return new WaitForSeconds(time);
		PlaySound(pop4);
		yield return new WaitForSeconds(time);
		PlaySound(pop4);
		yield return new WaitForSeconds(time);
		PlaySound(pop5);
		yield return new WaitForSeconds(time);
		PlaySound(pop5);
		yield return new WaitForSeconds(time);
		PlaySound(pop6);
		yield return new WaitForSeconds(time);
		PlaySound(pop6);
		yield return new WaitForSeconds(time);
		PlaySound(pop7);
		yield return new WaitForSeconds(time);
		PlaySound(pop7);
		yield return new WaitForSeconds(time);
		PlaySound(pop8);
		yield return new WaitForSeconds(time);
		PlaySound(pop8);
		yield return new WaitForSeconds(time);
	}



	public void SetCurrentLevelClip(AudioClip audio)
	{
		currentLevelClip = audio;
	}

	public void PlayCurrenLevelClip()
	{
		PlaySound(currentLevelClip, 0.7f, true, true);
	}

	void PlaySound(AudioClip sound, float volume = 0.7f, bool force = false, bool stop = false)
	{
		// stop other outputs to avoid overlaping
		if (stop)
			audio.Stop();
		if (isSilent && !force)
			return;
		if (audio == null)
		{
			Debug.LogError("audio is null");
			return;
		}
		audio.PlayOneShot(sound, volume);
	}
}
