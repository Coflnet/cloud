﻿using System;
using Coflnet;
using System.Collections.Generic;
using MessagePack;

namespace Coflnet.Client.Messaging
{
	/// <summary>
	/// A chat is between two parties
	/// </summary>
	[MessagePackObject]
	public class Chat : IChat
	{
		[Key(0)]
		public ChatMember partner;

		[Key(1)]
        public string Name {get;set; }

		[IgnoreMember]
        public SourceReference ID => partner.userId;

		[Key(2)]
        public long lastMessageIndex {get;set;}


		public Chat(ChatMember partner)
		{
			this.partner = partner;
		}

        public void SendMessage(string message)
		{
			var msg = new ChatMessage(message);
			CoflnetCore.Instance.SendCommand<ChatMessageCommand,ChatMessage>(partner.userId,msg);
		}

		public long MessageCount{get; set;}

	}

	[MessagePackObject]
    public class GroupChat : IChat
    {
		[Key(0)]
		public SourceReference GroupId;

		[IgnoreMember]
        public string Name 
		{ 
			get
			{
				return ReferenceManager.Instance.GetResource<GroupChatResource>(GroupId).Name;
			} set{
				// this does currently not distribute
				ReferenceManager.Instance.GetResource<GroupChatResource>(GroupId).Name = value;
			}
		}

		[IgnoreMember]
        public SourceReference ID => GroupId;

		[Key(1)]

        public long lastMessageIndex{get; set;}
		[Key(2)]
        public long MessageCount{get; set;}

        public void SendMessage(string message)
        {
            throw new NotImplementedException();
        }
    }

	[MessagePack.Union(0, typeof(Chat))]
    [MessagePack.Union(1, typeof(GroupChat))]
    public interface IChat
	{
		void SendMessage(string message);
		string Name {get;set;}

		SourceReference ID {get;}

		/// <summary>
		/// The index of the latest received message.
		/// Either choosen by the chat resource on the server or the other party
		/// </summary>
		/// <value></value>
		long lastMessageIndex {get;}

		/// <summary>
		/// The total count of messages in the chat
		/// </summary>
		/// <value></value>
		long MessageCount {get;}
	}


	public class GroupChatResource : Referenceable {

		private static CommandController chatCommands;

		public List<ChatMember> Members;

		public string Name;



        public override CommandController GetCommandController()
        {
            return chatCommands;
        }

		public override void ExecuteCommand(MessageData data, Command command)
		{
			// TODO distribute
			foreach (var member in Members)
			{
				var newData = new MessageData(data);
				newData.rId = member.userId;
				CoflnetCore.Instance.SendCommand(newData);
			}
		}
	}


	public enum ChatRole
	{
		Member,
		Admin = 1,
		Owner = 2
	}


[MessagePackObject]
	public class ChatMember
	{
		[Key(0)]
		public SourceReference userId;
		[Key(1)]
		public byte[] receiveKey;
		/// <summary>
		/// The index of the last message received and vertified 
		/// and the index of the ratchet receiveKey
		/// </summary>
		[Key(2)]
		public long lastMessageIndex;
		[Key(3)]
		public ChatRole Role;

		public ChatMember(SourceReference userId) : this(userId,new byte[0])
		{
		}
		public ChatMember(SourceReference userId,byte[] receiveKey)
		{
			this.userId = userId;
			this.receiveKey = receiveKey;
		}
		public ChatMember()
		{

		}
}
}