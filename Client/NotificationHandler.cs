using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coflnet.Client;
using System.Collections.Concurrent;

namespace Coflnet.Client
{

	public class NotificationHandler
	{

		ConcurrentQueue<AlertItem> alerts = new ConcurrentQueue<AlertItem>();

		public delegate void NewNotification(AlertItem item, NotificationHandler handler);
		public event NewNotification OnNewNotification;

		public static NotificationHandler Instance { get; }

		private bool showing = false;

		static NotificationHandler()
		{
			Instance = new NotificationHandler();
		}


		void AfterLoad()
		{
			string welcome = LocalizationManager.Instance.GetTranslation("welcome_to_game");
			AddMessageToAlertStream(welcome);
		}



		public void AddMessageToAlertStream(string text)
		{
			alerts.Enqueue(new AlertItem(text));

			TryShowNextAlert();
		}


		public void AddMessageToStreamAsync(string text)
		{
			alerts.Enqueue(new AlertItem(text));
		}

		/// <summary>
		/// Tries to show the next alert.
		/// </summary>
		public void TryShowNextAlert()
		{
			if (showing)
				return;
			showing = true;
			AlertItem result;
			alerts.TryDequeue(out result);
			if (result != null)
				OnNewNotification.Invoke(result, this);
		}


		public void AddActionAlert(string text, System.Action action, string actionText = "OK")
		{
			alerts.Enqueue(new AlertItem(text, action, actionText));
			if (!showing)
				TryShowNextAlert();
		}


	}

	public class AlertItem
	{
		public string text;
		public System.Action action;
		public string buttonText;

		public AlertItem(string text)
		{
			this.text = text;
		}

		public AlertItem(string text, System.Action action, string buttonText)
		{
			this.text = text;
			this.action = action;
			this.buttonText = buttonText;
		}
	}
}

