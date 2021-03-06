﻿using System;
using System.Collections.Generic;
using System.Text;
using Coflnet;
using System.Linq;

namespace Coflnet
{
	/// <summary>
	/// Old name for the I18nController
	/// </summary>
	public class LocalizationManager : I18nController
	{

	}


	public class I18nController
	{
		private event Action localLoadedCallBack;
		public bool loadDone { get; private set; }

		/// <summary>
		/// Gets or sets the translations.
		/// </summary>
		/// <value>The translations as plain string.</value>
		public Dictionary<string, string> Translations { get; protected set; }
		/// <summary>
		/// en-US format locale
		/// </summary>
		public string Locale { get; protected set; }

		public static I18nController Instance { get; }

		static I18nController()
		{
			Instance = new I18nController();
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

		/// <summary>
		/// Gets a translation.
		/// Allows one key value pair that is replaced within the translated string.
		/// </summary>
		/// <param name="key">For wich to search for a translation</param>
		/// <param name="valueKey">The key within the translation to translate</param>
		/// <param name="value">The value to replace it with (can also be another translation key)</param>
		/// <returns>The localized text</returns>
		public string GetTranslation(string key, string valueKey, string value){
			return GetTranslation(key, new KeyValuePair<string,string>(valueKey,value));
		}

		public void AddTranslations(Dictionary<string, string> values)
		{
			foreach (var item in values)
			{
				Translations.Add(item.Key, item.Value);
			}
		}

		/// <summary>
		/// Has to be invoked from the outside to allow for additional translations to be inserted before.
		/// Is invoked on either ServerCore or CoflnetClient.Init()
		/// </summary>
		public void LoadCompleted()
		{
			loadDone = true;
			localLoadedCallBack?.Invoke();
		}

		/// <summary>
		/// Adds a load callback to allow async translation loading.
		/// If all translations are allready loaded the callback will be invoked imediatly.
		/// The callback will be removed after invocation has happened once.
		/// </summary>
		/// <param name="callback">Callback to call when translations are ready.</param>
		public void AddLoadCallback(Action callback)
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




		protected void LoadTranslations()
		{
			string key = $"translation_{ConfigController.UserSettings.Locale}";
			DataController.Instance.LoadObjectAsync<Dictionary<string, string>>(key, result =>
			{
				AddTranslations(result);
			});
		}
	}

}
