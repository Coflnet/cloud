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
        SetupStartController.Instance.RedoSetup (true);
        PrivacyService.Instance.privacyScreen = new AcceptAllScreen ();
        DevCore.Init (new SourceReference (1, 1, 1, 0), true);

        Debug.Log (ConfigController.UserSettings.userId);
        // userid exists client side
        Assert.NotNull (ConfigController.Users.Find ((u) => u.userId == ConfigController.UserSettings.userId));

        // exists server side
        CoflnetUser user;
        ReferenceManager.Instance.TryGetResource<CoflnetUser> (ConfigController.UserSettings.userId, out user);
        Assert.NotNull (user);
    }

    [Test]
    public void FirstStartupStoreValue () {
        SetupStartController.Instance.RedoSetup (true);
        PrivacyService.Instance.privacyScreen = new AcceptAllScreen ();
        DevCore.Init (new SourceReference (1, 1, 1, 0), true);

        Debug.Log (ConfigController.UserSettings.userId);

        CoflnetCore.Instance
            .SendCommand<SetUserKeyValue, KeyValuePair<string, string>> (
                ConfigController.ActiveUserId,
                new KeyValuePair<string, string> ("mykey", "avalue"));

        CoflnetCore.Instance.SendCommand<GetUserKeyValue, string> (ConfigController.ActiveUserId, "mykey", (d) => {

        });
    }
}