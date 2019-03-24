using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coflnet;
using Coflnet.Client;
using MessagePack;
using System.Collections.Concurrent;
using System.IO;

public class CoflnetMain : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        Coflnet.Client.NotificationHandler.Instance.NotificationDisplay = NotificationHandler.instance;
        Coflnet.Client.PrivacyService.Instance.privacyScreen = PrivacyController.instance;
        ClientCore.ClientInstance.SetApplicationId("abcdefgabcdefgabcdefg");
        ClientCore.Init();

        StartCoroutine(LoadTranslations());
    }

    void OnApplicationExit()
    {
        ClientCore.Stop();
    }


    void OnApplicationPause()
    {
        ClientCore.Save();
    }




    IEnumerator LoadTranslations()
    {

        string fileName = ConfigController.UserSettings.Locale + ".json";
        string filePath = Path.Combine(UnityEngine.Application.streamingAssetsPath, fileName);
        string dataAsJson = " ";
        if (filePath.Contains("://"))
        {
            WWW www = new WWW(filePath);
            yield return www;
            dataAsJson = www.text;
        }
        else
            dataAsJson = File.ReadAllText(filePath);
        LocalizationData loadedData = JsonUtility.FromJson<LocalizationData>(dataAsJson);

        for (int i = 0; i < loadedData.items.Length; i++)
        {
            if (!Coflnet.LocalizationManager.Instance.Translations.ContainsKey(loadedData.items[i].key))
                Coflnet.LocalizationManager.Instance.Translations.Add(loadedData.items[i].key, loadedData.items[i].value);
        }


        // done
        Coflnet.LocalizationManager.Instance.LoadCompleted();
    }
}
