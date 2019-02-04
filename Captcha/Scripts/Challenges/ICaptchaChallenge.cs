using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICaptchaChallenge
{
	/// <summary>
	/// Displaies a challenge.
	/// </summary>
	/// <param name="challengeData">Challenge data.</param>
	void DisplayChallenge(CaptchaChallenge challengeData, GameObject container);

	/// <summary>
	/// Gets the challenge data. Called after the user clicks okay
	/// </summary>
	/// <returns>The challenge data ready for submit.</returns>
	string GetChallengeData();

	/// <summary>
	/// Gets the slug for this challenge
	/// </summary>
	/// <returns>The slug.</returns>
	string GetSlug();

	/// <summary>
	/// Callback executed when additional data has been loaded.
	/// Should hide the loading animation on the <see cref="CaptchaController"/>
	/// </summary>
	void LoadCallback();
}
