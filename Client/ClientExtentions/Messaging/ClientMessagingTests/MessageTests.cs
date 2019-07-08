using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet.Client.Messaging;
using Coflnet;

public class MessageTests {

    [Test]
    public void StoreMessages() {
        var message1 = new LocalChatMessage();
        var message2 = new ChatMessage();

        ChatService.Instance.SaveMessage(message1);

    }
}
