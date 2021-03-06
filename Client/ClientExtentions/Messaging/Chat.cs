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
        public EntityId ID => partner.userId;

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

		[Key(3)]
		public long MessageCount{get; set;}

	}

	[MessagePackObject]
    public class GroupChat : IChat
    {
		/// <summary>
		/// The id of the chat
		/// </summary>
		[Key(0)]
		public EntityId GroupId;

        public GroupChat()
        {
        }

		/// <summary>
		/// Generates a new instance of a <see cref="GroupChat"/>
		/// </summary>
		/// <param name="groupId"></param>
		public GroupChat(EntityId groupId)
        {
			GroupId = groupId;
        }

        [IgnoreMember]
        public string Name 
		{ 
			get
			{
				return EntityManager.Instance.GetEntity<GroupChatResource>(GroupId).Name;
			} set{
				// this does currently not distribute
				EntityManager.Instance.GetEntity<GroupChatResource>(GroupId).Name = value;
			}
		}

		[IgnoreMember]
        public EntityId ID => GroupId;

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

		EntityId ID {get;}

		/// <summary>
		/// The index of the latest received message.
		/// Either choosen by the chat resource on the server or the other party
		/// </summary>
		/// <value></value>
		long lastMessageIndex {get;set;}

		/// <summary>
		/// The total count of messages in the chat
		/// </summary>
		/// <value></value>
		long MessageCount {get;set;}
	}


	public class GroupChatResource : Entity {

		private static CommandController chatCommands;

		public List<ChatMember> Members;

		public string Name;



        public override CommandController GetCommandController()
        {
            return chatCommands;
        }

		public override void ExecuteCommand(CommandData data, Command command)
		{
			// TODO distribute
			foreach (var member in Members)
			{
				var newData = new CommandData(data);
				newData.Recipient = member.userId;
				CoflnetCore.Instance.SendCommand(newData);
			}
		}
	}

    public class CreateGroupChat : CreationCommand
    {
        public override string Slug => "createGroupChat";

        public override Entity CreateResource(CommandData data)
        {
            var chat = new GroupChatResource();
			var options = data.GetAs<Params>();

			chat.Name = options.Name;

			foreach (var user in options.Members)
			{
				chat.Members.Add(new ChatMember(user));
			}

			return chat;
        }

        protected override CommandSettings GetSettings()
        {
            return new CommandSettings();
        }

		[MessagePackObject]
		public class Params : CreationParamsBase
		{
			[Key(2)]
			public string Name;
			[Key(3)]
			public List<EntityId> Members;

			/// <summary>
			/// Creates a new instance of the command Params
			/// </summary>
			/// <param name="members">The members to add to the group</param>
			/// <param name="name">The name the group will have</param>
            public Params(List<EntityId> members, string name)
            {
				this.Members = members;
				this.Name = name;
            }
        }
    }

    public class GroupChatMessageOut : Command
    {
        public override string Slug => "msg";

        public override void Execute(CommandData data)
        {
			// distribute the message
            foreach (var item in data.GetTargetAs<GroupChatResource>().Members)
			{
				data.CoreInstance.SendCommand<GroupChatMessageIn,byte[]>(item.userId,data.message,0,data.Recipient);
			}
        }

        protected override CommandSettings GetSettings()
        {
            return new CommandSettings(true,false,false,IsChatMember.Instance);
        }
    }


	public class GroupChatMessageIn : Command
	{
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
		public override void Execute(CommandData data)
		{
			
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		protected override CommandSettings GetSettings()
		{
			return new CommandSettings( );
		}
		/// <summary>
		/// The globally unique slug (short human readable id) for this command.
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug => "groupMsg";
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
		public EntityId userId;
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

		public ChatMember(EntityId userId) : this(userId,new byte[0])
		{
		}
		public ChatMember(EntityId userId,byte[] receiveKey)
		{
			this.userId = userId;
			this.receiveKey = receiveKey;
		}
		public ChatMember()
		{

		}
}
}