using Coflnet.Dev;
using NUnit.Framework;
using Coflnet;
using Coflnet.Client.Messaging;
using MessagePack;

public class MessageDeliveryTests {


    /// <summary>
    /// 
    /// </summary>
    [Test]
    public void MessageDeliverySameServerConnectedTest() {
        var alice = new EntityId(1,123);
        var bob = new EntityId(1,555);
        DevCore.Init(alice);

        
        DevCore.DevInstance.AddClientCore(bob).OnMessage = m => {
            // received data is of type msg
            Assert.AreEqual("msg",m.Type);
            return false;
        };


        CoflnetCore.Instance.SendCommand(
            CommandData.CreateCommandData<ChatMessageCommand,string>(
                bob,"hi",0,alice));
    }
    
    [Test]
    public void MessageDeliveryMultipleServerConnectedTest() {
        var alice = new EntityId(1,123);
        var bob = new EntityId(2,555);
        DevCore.Init(alice);
        DevCore.DevInstance.AddClientCore(bob).OnMessage = m => {
            // when bob receives the message make sure it is valid
            Assert.AreEqual("msg",m.Type);
            return false;
        };

        CoflnetCore.Instance.SendCommand(
            CommandData.CreateCommandData<ChatMessageCommand,string>(
                bob,"hi",0,alice));
    }

    [Test,Timeout(1000)]
    public void UpdatePropagationMultipleServerConnectedTest() {

        var aliceDeviceId = new EntityId(1,9);
        var bobDeviceId = new EntityId(2,8);
        DevCore.Init(aliceDeviceId);
        // add bob  and bobs server
        DevCore.DevInstance.AddServerCore(bobDeviceId.FullServerId);
        DevCore.DevInstance.AddClientCore(bobDeviceId,true).OnMessage = m => {
            if(m.Type == "UpdateUserName")
                Assert.AreEqual("bob",m.GetAs<string>());
            return true;
        };

        var alice = DevCore.DevInstance.simulationInstances[aliceDeviceId].core;

        alice.CloneAndSubscribe(bobDeviceId);
        
        var msg = new CommandData(bobDeviceId,0,
                            MessagePackSerializer.Serialize("coflnet.app"),"AddInstalledApp");

        throw new System.Exception("The next line never finishes");
        //CoflnetCore.Instance.SendCommand(msg);

           
          //   CommandData.CreateCommandData<UpdateUserNameCommand,string>(
          //       bobDeviceId,"bob",0,aliceDeviceId));


        // alice should now also know the new app of bob
        Assert.IsTrue(alice.EntityManager.GetEntity<Device>(bobDeviceId).InstalledApps.Contains("coflnet.app"));

    }


}
