using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Coflnet.Client;

namespace Coflnet.Client
{
	public class PrivacyService
	{
		/// <summary>
		/// Gets an instance of this class.
		/// </summary>
		/// <value>The instance.</value>
		public static PrivacyService Instance { get; }
		/// <summary>
		/// Contains explicit acti/deactivations of privacy options
		/// </summary>
		/// <value>The settings.</value>
		public Dictionary<string, bool> Settings { get; private set; }

		private System.Action _whenDone;

		public IPrivacyScreen privacyScreen { get; set; }

		static PrivacyService()
		{
			Instance = new PrivacyService();
		}



		public void ShowScreen(System.Action whenDone)
		{
			_whenDone = whenDone;
			privacyScreen.ShowScreen(DecissionMade);
		}

		public void DecissionMade(int level)
		{
			// there probably isn't a user yet       
			// set options
			// todo

			Settings = new Dictionary<string, bool>();

			_whenDone?.Invoke();
		}

		/// <summary>
		/// Am I allowed to do a specific privacy related thing
		/// </summary>
		/// <returns><c>true</c>, if permission was granted, <c>false</c> otherwise.</returns>
		/// <param name="slug">Slug.</param>
		public bool AmIAllowedToDo(string slug)
		{
			bool value;
			if (UserService.Instance.GetUser().PrivacySettings.TryGetValue(slug, out value))
			{
				return value;
			}
			return false;
		}

		/// <summary>
		/// Sets the permission.*
		/// </summary>
		/// <param name="slug">Slug.</param>
		/// <param name="decission">If set to <c>true</c> decission.</param>
		public void SetPermission(string slug, bool decission)
		{
			UserService.Instance.GetUser().PrivacySettings[slug] = decission;
			CoflnetCore.Instance.SendCommand<UpdatePrivacySetting>
		}
	}


	public interface IPrivacyScreen
	{
		/// <summary>
		/// Shows the privacy screen.
		/// </summary>
		/// <param name="whenDone">Should be exectued with the coresponding privacy level when done.</param>
		void ShowScreen(System.Action<int> whenDone);
	}
}

