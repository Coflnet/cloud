using System;
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

        public void SendMessage(string message)
		{
			var msg = new ChatMessage(message);
			CoflnetCore.Instance.SendCommand<ChatMessageCommand,ChatMessage>(partner.userId,msg);
		}


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

	}


	public class GroupChatResource : Referenceable {

		private static CommandController chatCommands;

		public List<ChatMember> Members;

		public string Name;



        public override CommandController GetCommandController()
        {
            return chatCommands;
        }
	}


	public enum ChatRole
	{
		Member,
		Admin = 1,
		Owner = 2
	}


	public class ChatMember
	{
		public SourceReference userId;
		public byte[] receiveKey;
		/// <summary>
		/// The index of the last message received and vertified 
		/// and the index of the ratchet receiveKey
		/// </summary>
		public long lastMessageIndex;
		public ChatRole Role;

		public ChatMember(SourceReference userId)
		{
			this.userId = userId;
		}
		public ChatMember()
		{

		}
}
}