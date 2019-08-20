using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using NUnit.Framework;
using Coflnet.Client.Messaging;
using Coflnet;
using System;
using Coflnet.Client;
using System.Collections.Generic;
using System.Linq;

public class MessageTests {

    [Test]
    public void StoreMessages() {
        UserService.Instance.ChangeCurrentUser(new SourceReference(5,12));
        
        List<LocalChatMessage> messages = new List<LocalChatMessage>();

        var chat = CreateChat();

        var messageCount = 10;

        for (int i = 0; i < messageCount; i++)
        {
            messages.Add( new LocalChatMessage("hi",DateTime.Now,default(MessageReference),
            MessageReference.Next,ChatMessage.Type.Message){
                chatId = chat.ID
            });
        }

        foreach (var message in messages)
        {
            // saves the messages
            ChatService.Instance.AddMessage(message);
        }

        // test if they can be retreived
        foreach (var item in ChatService.Instance.GetMessages(chat,5,0))
        {
            // the messages are the ones saved
            Assert.Contains(item,messages);
        }

        // not more than the amount actually stored can be loaded
        Assert.AreEqual( messageCount,ChatService.Instance.GetMessages(chat,Int32.MaxValue,0).Count() );
    }


    /// <summary>
    /// Load the last 20 message from the last 5 days
    /// </summary>
        [Test]
    public void LoadMessagesWithOffsetSmal() {
        UserService.Instance.ChangeCurrentUser(new SourceReference(5,12));
        List<LocalChatMessage> messages = new List<LocalChatMessage>();
        var chat = CreateChat();
        var messageCount = 20;

        for (int i = 0; i < messageCount; i++)
        {
            // generate messages made in the last few days
            //var r = new System.Random();
            var creationTime = DateTime.Now.Subtract(new TimeSpan(4- i/4,0,0,i));

            messages.Add( new LocalChatMessage("hi",
                creationTime,
                default(MessageReference),
                new MessageReference(MessageReference.Next.Source,creationTime.Ticks),
                ChatMessage.Type.Message){
                chatId = chat.ID
            });
        }

        // save them 
        foreach (var message in messages)
        {
            ChatService.Instance.AddMessage(message);
        }

        int index = messageCount-1;

        // test if they can be retreived
        foreach (var item in ChatService.Instance.GetMessages(chat,5,0))
        {
            // the messages are the ones saved
            Assert.AreEqual(messages[index],item);
            index--;
        }


        // get some with offset
        foreach (var item in ChatService.Instance.GetMessages(chat,5,5))
        {
            // the messages are the ones saved
            Assert.AreEqual(messages[index],item);
            index--;
        }

        // get some with more offset
        foreach (var item in ChatService.Instance.GetMessages(chat,5,10))
        {
            // the messages are the ones saved
            Assert.AreEqual(messages[index],item);
            index--;
        }


        FileController.DeleteFolder("messages");
    }


    /// <summary>
    /// 2000 messages over the last week
    /// </summary>
            [Test]
    public void LoadMessagesWithOffsetBig() {
        // make sure the test conditions are met
        FileController.DeleteFolder("messages");
        FileController.Delete("chats");
        ChatService.Instance.DeleteAll();

        UserService.Instance.ChangeCurrentUser(new SourceReference(5,12));
        List<LocalChatMessage> messages = new List<LocalChatMessage>();
        var chat = CreateChat();
        var messageCount = 2000;

        for (int i = 0; i < messageCount; i++)
        {
            // generate messages made in the last few days
            //var r = new System.Random();
            var creationTime = DateTime.Now.Subtract(new TimeSpan(7- i*7/messageCount,0,0,i));

            messages.Add( new LocalChatMessage("hi",
                creationTime,
                default(MessageReference),
                new MessageReference(MessageReference.Next.Source,creationTime.Ticks),
                ChatMessage.Type.Message){
                chatId = chat.ID
            });
        }

        // save them 
        foreach (var message in messages)
        {
            ChatService.Instance.AddMessage(message);
        }

        int index = messageCount-1;

        // test if they can be retreived
        foreach (var item in ChatService.Instance.GetMessages(chat,10,0))
        {
            // the messages are the ones saved
            Assert.AreEqual(messages[index],item);
            index--;
        }


        // get some with offset
        foreach (var item in ChatService.Instance.GetMessages(chat,23,10))
        {
            // the messages are the ones saved
            Assert.AreEqual(messages[index],item);
            index--;
        }

        // get some with more offset
        foreach (var item in ChatService.Instance.GetMessages(chat,1500,33))
        {
            // the messages are the ones saved
            Assert.AreEqual(messages[index],item);
            index--;
        }


        FileController.DeleteFolder("messages");
    }



    private Chat CreateChat()
    {
        var chatId = new SourceReference(26,2);
        var chat = new Chat(new ChatMember(chatId));
        ChatService.Instance.AddChat(chat);
        return chat;
    }
}
