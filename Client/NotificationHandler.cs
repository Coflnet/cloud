using System.Collections;
using System.Collections.Generic;
using Coflnet.Client;
using System.Collections.Concurrent;

namespace Coflnet.Client
{

	public class NotificationHandler
	{
		ConcurrentQueue<AlertItem> alerts = new ConcurrentQueue<AlertItem>();

		/// <summary>
		/// Gets or sets the notification display used for displaying alerts
		/// </summary>
		/// <value>The notification display.</value>
		public INotificationDisplay NotificationDisplay { get; set; }

		public static NotificationHandler Instance { get; }

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

		/// <summary>
		/// Adds a translated message to the alert stream
		/// </summary>
		/// <param name="message">Message key to translate</param>
		/// <param name="values">Optional additional values</param>
		public void AddTranslatedAlert(string message, params KeyValuePair<string,string>[] values)
		{
			var translation = LocalizationManager.Instance.GetTranslation(message,values);
			AddMessageToAlertStream(translation);
		}


		public void AddMessageToStreamAsync(string text)
		{
			alerts.Enqueue(new AlertItem(text));
		}

		public AlertItem GetNextAlert()
		{
			AlertItem result;
			alerts.TryPeek(out result);

			return result;
		}

		/// <summary>
		/// Tries to show the next alert.
		/// Adds alerts to a queue if showing failed.
		/// </summary>
		public void TryShowNextAlert()
		{
			AlertItem result = GetNextAlert();

			if (result == null)
			{
				return;
			}
			if (NotificationDisplay.TryShowNextAlert(result))
			{
				// Showing was successful, remove it from the queue
				alerts.TryDequeue(out result);
			}
		}


		public void AddActionAlert(string text, System.Action action, string actionText = "OK")
		{
			alerts.Enqueue(new AlertItem(text, action, actionText));
			TryShowNextAlert();
		}


	}

	/// <summary>
	/// Representing an alert that is or will displayed to a user.
	/// </summary>
	public class AlertItem
	{
		/// <summary>
		/// Text displayed in the alert
		/// </summary>
		public string text;
		/// <summary>
		/// Custom action that will invoked when button is clicked
		/// </summary>
		public System.Action action;
		/// <summary>
		/// Custom text display on the action button
		/// </summary>
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

	/// <summary>
	/// Any Object capable of displaying an Notification.
	/// </summary>
	public interface INotificationDisplay
	{
		/// <summary>
		/// Tries the show next alert.
		/// Should return <see langword="false"/> if another Alert is already showing.
		/// When showing is done the next alert can be retreived with <see cref="NotificationHandler.GetNextAlert"/> 
		/// or <see cref="NotificationHandler.TryShowNextAlert"/>
		/// </summary>
		/// <returns><c>true</c>, if alert was shown, <c>false</c> otherwise.</returns>
		/// <param name="alertItem">Alert item.</param>
		bool TryShowNextAlert(AlertItem alertItem);
	}
}

