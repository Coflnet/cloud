using System;
using MessagePack;
using Coflnet.Client;
using System.Collections.Generic;

namespace Coflnet.Client.Messaging
{
	/// <summary>
	/// Interact with the chat service to receive and send messages
	/// </summary>
	public class ChatService  {

		/// <summary>
		/// The ChatManager is responsible for actually storing and reading chats
		/// </summary>
		IChatManager ChatManager;

		public Action<ChatMessage> OnReceiveMessage;

		public void ReceiveMessage(ChatMessage message)
		{

		}

		public void SendMessage(ChatMessage message)
		{

		}

		public ChatMessage GetMessage(MessageReference reference)
		{
			return new ChatMessage();
		}

		public IEnumerable<ChatMessage> GetMessages(SourceReference chat, long startIndex = 0, long endIndex = long.MaxValue)
		{
			yield break;
		}

		public IEnumerable<GroupChat> GetChats()
		{
			return ChatManager.GetChats();
		}
	
	}


	public interface IChatManager
	{
		IEnumerable<GroupChat> GetChats();
	}



	public class ChatMessage
{
	[Key(1)]
	public string content;
	/// <summary>
	/// The date time when this message was sent.
	/// </summary>
	[Key(2)]
	public DateTime timetamp;
	/// <summary>
	/// What if any message this one references 
	/// </summary>
	[Key(3)]
	public MessageReference refs;
	/// <summary>
	/// Unique message id consisting of server, sender and messageid
	/// </summary>
	[Key(0)]
	public MessageReference id;

	public enum Type { Message, Image, Audio, Video, Emoji, Invite, Call };
	[Key(4)]
	public Type type;

	[MessagePack.IgnoreMember]
	public SourceReference sender
	{
		get
		{
			return id.Source;
		}
	}


	public ChatMessage(string content, DateTime timetamp, long refs, MessageReference id, Type type)
	{
		this.content = content;
		this.timetamp = timetamp;
		this.refs = refs;
		this.id = id;
		this.type = type;
	}

	public ChatMessage(string content) : this(content, DateTime.Now, -1, MessageReference.Next, Type.Message)
	{
	}

	public ChatMessage()
	{

	}


	/// <summary>
	/// Ises the mine.
	/// </summary>
	/// <returns><c>true</c>, if mine was ised, <c>false</c> otherwise.</returns>
	public bool IsMine()
	{
		return id.Source == ConfigController.UserSettings.userId;
	}
}

/// <summary>
/// Local chat message contains additional iformation like received and watched noticements
/// </summary>
[MessagePackObject]
public class LocalChatMessage : ChatMessage
{
	public enum Status
	{
		Received,
		ReceivedAndRead
	}

	[Key(10)]
	Dictionary<SourceReference, Status> States;

	/// <summary>
	/// Wherether or not the message as been sent
	/// </summary>
	[Key(11)]
	public bool Sent;

	/// <summary>
	/// The chat id this message coresponds to.
	/// </summary>
	[Key(12)]
	public SourceReference chat;

	public LocalChatMessage(string content, DateTime timetamp, long refs, MessageReference id, Type type, Dictionary<SourceReference, Status> states = null) : base(content, timetamp, refs, id, type)
	{
		States = states;
	}

	public LocalChatMessage()
	{
	}
}
}

