using Coflnet.Dev;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet;
using Coflnet.Client;
using Coflnet.Client.Messaging;
using Coflnet.Core.User;

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
        var alice = new SourceReference(1,123);
        var bob = new SourceReference(2,555);
        DevCore.Init(alice);
        DevCore.DevInstance.AddClientCore(bob).OnMessage = m => {
            Assert.AreEqual("Alice",m.GetAs<string>());
            return true;
        };

        CoflnetCore.Instance.SendCommand(
            MessageData.CreateMessageData<UpdateUserNameCommand,string>(
                alice,"Alice",0,bob));
    }


}
