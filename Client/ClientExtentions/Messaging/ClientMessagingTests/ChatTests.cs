using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet.Client.Messaging;
using Coflnet;

public class ChatTests {

    [Test]
    public void AddChat() {
        var chat =new Chat(new ChatMember(new SourceReference(5,4)));

        ChatService.Instance.AddChat(chat);

        ChatService.Instance.Save();

        // set new instance of the chatmanager
        // this forces the manager to load the chats
        ChatService.Instance.ChatManager = new CoflnetChatManager();

        var loadedChat = ChatService.Instance.GetChat(chat.ID);
        
        Assert.AreEqual(chat.ID,loadedChat.ID);

        // clean up
        ChatService.Instance.ChatManager.RemoveChat(loadedChat);
        ChatService.Instance.Save();

    }
    [Test]
    public void RemoveChat() {
        var chat =new Chat(new ChatMember(new SourceReference(5,4)));
        ChatService.Instance.AddChat(chat);

        ChatService.Instance.Save();

        // set new instance of the chatmanager
        // this forces the manager to load the chats
        ChatService.Instance.ChatManager = new CoflnetChatManager();

        ChatService.Instance.ChatManager.RemoveChat(chat);
        ChatService.Instance.Save();

        // chat can't be found
        Assert.Throws<ChatNotFoundException>(()=>{
            var loadedChat = ChatService.Instance.GetChat(chat.ID);
        });
        
    }

}
