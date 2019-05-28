using System;
using System.Collections;
using System.Collections.Generic;
using Coflnet;
using Coflnet.Client;
using Coflnet.Dev;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class FirstStartupTest {

    [Test]
    public void FirstStartupTestSimplePasses () {
        DevCore.Init (new SourceReference (1, 1, 1, 123456));
    }

    private class AcceptAllScreen : IPrivacyScreen {
        public void ShowScreen (Action<int> whenDone) {
            whenDone (3);
        }
    }

    [Test]
    public void FirstStartupRegisterLoginTest () {
        FirstStartSetupController.Instance.RedoSetup (true);
        PrivacyService.Instance.privacyScreen = new AcceptAllScreen ();
        var serverId = new SourceReference (1, 1, 1, 0);
        DevCore.Init (serverId, true);



        Debug.Log (ConfigController.UserSettings.userId);
        // userid exists client side
        Assert.NotNull (ConfigController.Users.Find ((u) => u.userId == ConfigController.UserSettings.userId));

        // exists server side
        CoflnetUser user;
        // ActiveUserId should change on first register
        DevCore.DevInstance
            .simulationInstances[serverId]
            .core.ReferenceManager
            .TryGetResource<CoflnetUser> (ConfigController.ActiveUserId, out user);
        
        Assert.NotNull (user);


        // exists client side
        CoflnetUser userOnClient;
        // ActiveUserId should change on first register
        DevCore.DevInstance
            .simulationInstances[serverId]
            .core.ReferenceManager
            .TryGetResource<CoflnetUser> (ConfigController.ActiveUserId, out userOnClient);
        
        Assert.NotNull (userOnClient);
    }

    [Test]
    public void FirstStartupStoreValue () {
        FirstStartSetupController.Instance.RedoSetup (true);
        PrivacyService.Instance.privacyScreen = new AcceptAllScreen ();
        var serverId = new SourceReference (1, 1, 1, 0);
        DevCore.Init (serverId, true);

        Debug.Log (ConfigController.UserSettings.userId);

        var valueToStore = "abc123Hellou :D";

        var data =  MessageData.CreateMessageData<SetUserKeyValue,KeyValuePair<string, string>> (
                ConfigController.ActiveUserId,
                new KeyValuePair<string, string> ("mykey", valueToStore));
                data.sId = ConfigController.ActiveUserId;
        CoflnetCore.Instance
            .SendCommand(data);

        foreach (var item in DevCore.DevInstance.simulationInstances.Keys)
        {
            Debug.Log ($"having {item}");
        }

        CoflnetCore.Instance.SendCommand<GetUserKeyValue, string> (ConfigController.ActiveUserId, "mykey",ConfigController.ActiveUserId, (d) => {
            Assert.AreEqual(valueToStore,d.GetAs<string>());
            UnityEngine.Debug.Log("response received and validated successfully");
        });
    }
}