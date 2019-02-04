using System;
using System.Collections.Generic;
using System.Text;
using Coflnet;
using System.Linq;

namespace Coflnet
{
	public class LocalizationManager
	{
		public delegate void LocalizationLoaded();
		private event LocalizationLoaded localLoadedCallBack;
		private bool loadDone;

		/// <summary>
		/// Gets or sets the translations.
		/// </summary>
		/// <value>The translations as plain string.</value>
		public Dictionary<string, string> Translations { get; protected set; }
		/// <summary>
		/// en-US format locale
		/// </summary>
		public string Locale { get; protected set; }

		public static LocalizationManager Instance { get; }

		static LocalizationManager()
		{
			Instance = new LocalizationManager();
		}

		/// <summary>
		/// Gets the translation allows additional key value pairs that are replaced within the string.
		/// </summary>
		/// <returns>The translation.</returns>
		/// <param name="key">Key for the atranslation.</param>
		/// <param name="values">Values to replace within the translation, will also be translated.</param>
		public string GetTranslation(string key, params KeyValuePair<string, string>[] values)
		{
			string result;
			Translations.TryGetValue(key, out result);

			if (result == null)
			{
				result = key;
			}
			// try to replace any key value pairs
			foreach (var item in values)
			{
				result.Replace(item.Key, GetTranslation(item.Value));
			}
			return result;
		}

		public void AddTranslations(Dictionary<string, string> values)
		{
			foreach (var item in values)
			{
				Translations.Add(item.Key, item.Value);
			}
		}

		public void LoadCompleted()
		{
			loadDone = true;
			localLoadedCallBack.Invoke();
		}

		/// <summary>
		/// Adds a load callback to allow async translation loading.
		/// If all translations are allready loaded the callback will be invoked imediatly.
		/// The callback will be removed after invocation has happened once.
		/// </summary>
		/// <param name="callback">Callback to call when translations are ready.</param>
		public void AddLoadCallback(LocalizationLoaded callback)
		{
			if (loadDone)
			{
				callback.Invoke();
				return;
			}

			// self remove the callback
			localLoadedCallBack += () =>
			 {
				 localLoadedCallBack -= callback;
			 };

			localLoadedCallBack += callback;
		}
	}

}
