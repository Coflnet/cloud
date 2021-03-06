﻿using System;
using System.Collections.Generic;
using Coflnet;
using Coflnet.Client;
using Coflnet.Dev;
using NUnit.Framework;

public class FirstStartupTest {

    [Test]
    public void FirstStartupTestSimplePasses () {
        DevCore.Init (new EntityId (1, 1, 1, 123456));
    }

    private class AcceptAllScreen : IPrivacyScreen {
        public void ShowScreen (Action<int> whenDone) {
            whenDone (3);
        }
    }


    [Test]
    public void DeviceAndInstallRegister()
    {
        FirstStartSetupController.Instance.RedoSetup (true);
        var serverId = new EntityId (1, 1, 1, 0);
        // start
        DevCore.Init (serverId, false,true);
        // servercore won't receive commands
        //DevCore.DevInstance.simulationInstances[serverId].IsConnected = false;

        //var clientCore = DevCore.DevInstance.simulationInstances[serverId];

        // start the client
        ClientCore.Init();

        // Device and Installation should now exist
        Assert.AreNotEqual(default(EntityId),ConfigController.DeviceId);
        Assert.AreNotEqual(default(EntityId),ConfigController.InstallationId);

        // enable network
        DevCore.DevInstance.simulationInstances[serverId].IsConnected = true;

        // cause an update that will attempt to send offline queue

        // Device and Installation should have online ids

        Logger.Log($"DeviceId: {ConfigController.DeviceId}");
        Logger.Log($"InstallationId: {ConfigController.InstallationId}");
    }

    [Test]
    public void FirstStartupRegisterLoginTest () {
        PrivacyService.Instance.privacyScreen = new AcceptAllScreen ();
        var serverId = new EntityId (1, 1, 1, 0);
        DevCore.Init (serverId, true);



        Logger.Log ("The new user has the id: " + UserService.Instance.CurrentUserId);
        Logger.Log($"The Active user is: {ConfigController.ActiveUserId}");
        // userid exists client side
        Assert.NotNull (UserService.Instance.CurrentUser);

        // exists server side
        CoflnetUser user;
        // ActiveUserId should change on first register
        DevCore.DevInstance
            .simulationInstances[serverId]
            .core.EntityManager
            .TryGetEntity<CoflnetUser> (ConfigController.ActiveUserId, out user);
        
        Assert.NotNull (user);


        // exists client side
        CoflnetUser userOnClient;
        // ActiveUserId should change on first register
        DevCore.DevInstance
            .simulationInstances[serverId]
            .core.EntityManager
            .TryGetEntity<CoflnetUser> (ConfigController.ActiveUserId, out userOnClient);
        
        Assert.NotNull (userOnClient);


        FirstStartSetupController.Instance.RedoSetup (true);
    }

    [Test]
    public void FirstStartupStoreValue () {
        FirstStartSetupController.Instance.RedoSetup (true);
        PrivacyService.Instance.privacyScreen = new AcceptAllScreen ();
        var serverId = new EntityId (1, 1, 1, 0);
        DevCore.Init (serverId, true);

        Logger.Log (ConfigController.UserSettings.userId);

        var valueToStore = "abc123Hellou :D";

        // sometimes it comes to errors when the test is run alone and the static constructor 
        // of CoflnetUser didn't register the SetUserKeyValue Command

        var data =  CommandData.CreateCommandData<SetUserKeyValue,KeyValuePair<string, string>> (
                ConfigController.ActiveUserId,
                new KeyValuePair<string, string> ("mykey", valueToStore));
                data.SenderId = ConfigController.ActiveUserId;
        CoflnetCore.Instance
            .SendCommand(data);

        foreach (var item in DevCore.DevInstance.simulationInstances.Keys)
        {
            Logger.Log ($"having {item}");
        }

        CoflnetCore.Instance.SendCommand<GetUserKeyValue, string> (ConfigController.ActiveUserId, "mykey",ConfigController.ActiveUserId, (d) => {
            Assert.AreEqual(valueToStore,d.GetAs<string>());
                    });
    }


    
}