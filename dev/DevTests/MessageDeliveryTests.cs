using Coflnet.Dev;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet;
using Coflnet.Client;
using Coflnet.Client.Messaging;
using Coflnet.Core.User;
using MessagePack;

public class MessageDeliveryTests {


    /// <summary>
    /// 
    /// </summary>
    [Test]
    public void MessageDeliverySameServerConnectedTest() {
        var alice = new SourceReference(1,123);
        var bob = new SourceReference(1,555);
        DevCore.Init(alice);

        
        DevCore.DevInstance.AddClientCore(bob).OnMessage = m => {
            // received data is of type msg
            Assert.AreEqual("msg",m.t);
            return false;
        };


        CoflnetCore.Instance.SendCommand(
            MessageData.CreateMessageData<ChatMessageCommand,string>(
                bob,"hi",0,alice));
    }
    
    [Test]
    public void MessageDeliveryMultipleServerConnectedTest() {
        var alice = new SourceReference(1,123);
        var bob = new SourceReference(2,555);
        DevCore.Init(alice);
        DevCore.DevInstance.AddClientCore(bob).OnMessage = m => {
            // when bob receives the message make sure it is valid
            Assert.AreEqual("msg",m.t);
            return false;
        };

        CoflnetCore.Instance.SendCommand(
            MessageData.CreateMessageData<ChatMessageCommand,string>(
                bob,"hi",0,alice));
    }

    [Test]
    public void UpdatePropagationMultipleServerConnectedTest() {
        var aliceDeviceId = new SourceReference(1,9);
        var bobDeviceId = new SourceReference(2,8);
        DevCore.Init(aliceDeviceId);
        // add bob  and bobs server
        DevCore.DevInstance.AddServerCore(bobDeviceId.FullServerId);
        DevCore.DevInstance.AddClientCore(bobDeviceId,true).OnMessage = m => {
            if(m.t == "UpdateUserName")
                Assert.AreEqual("bob",m.GetAs<string>());
            return true;
        };

        var alice = DevCore.DevInstance.simulationInstances[aliceDeviceId].core;

        alice.CloneAndSubscribe(bobDeviceId);
        


        CoflnetCore.Instance.SendCommand(new MessageData(bobDeviceId,0,
                            MessagePackSerializer.Serialize("coflnet.app"),"AddInstalledApp"));

           
          //   MessageData.CreateMessageData<UpdateUserNameCommand,string>(
          //       bobDeviceId,"bob",0,aliceDeviceId));


        // alice should now also know the new app of bob
        Assert.IsTrue(alice.ReferenceManager.GetResource<Device>(bobDeviceId).InstalledApps.Contains("coflnet.app"));

    }


}
