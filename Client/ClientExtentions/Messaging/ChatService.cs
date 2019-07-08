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
		public IChatManager ChatManager {get;set;}

		/// <summary>
		/// The MessageManager is responsible for actually storing messages
		/// </summary>
		public IMessageManager MessageManager{get;set;}

		public static ChatService Instance;

		public ChatService() : this(new CoflnetChatManager(),new CoflnetMessageManager())
		{

		}

		public ChatService(IChatManager chatManager,IMessageManager messageManager)
		{
			this.ChatManager = chatManager;
			this.MessageManager = messageManager;
		}

		static ChatService()
		{
			Instance = new ChatService();
		}

		public event Action<ChatMessage> OnReceiveMessage;

		public void ReceiveMessage(ChatMessage message,SourceReference chatId)
		{
			// this is the chat in which it was sent
			var chat = ChatManager.GetChat(chatId);
				var userName = "";//UserService.Instance.GetUser(message.mId.source)

			// this is the real sender (user)
			if(message.id.Source == chatId)
			{
				// this is a private chat
				NotificationHandler.Instance.AddTranslatedAlert(
					"x_wrote_you_message",
					new KeyValuePair<string,string>("user_name",userName));
			} else {
				// we are in a group chat
				var chatName = chat.Name;

				NotificationHandler.Instance.AddTranslatedAlert(
					"x_wrote_you_message_in_x",
					new KeyValuePair<string,string>("user_name",userName),
					new KeyValuePair<string,string>("chat_name",chatName));
			}

			// save it
			// TODO
			
		}

		public void SaveMessage(LocalChatMessage message)
		{
			MessageManager.SaveMessage(message);
		}

		public void SendMessage(ChatMessage message, SourceReference target)
		{
			CoflnetCore.Instance.SendCommand<ChatMessageCommand,ChatMessage>(target,message);
			CoflnetCore.Instance.SendCommand<ChatMessageCommand,ChatMessage>(target,message);
		}



		public ChatMessage GetMessage(MessageReference reference)
		{
			return new ChatMessage();
		}

		public IEnumerable<ChatMessage> GetMessages(SourceReference chat, long startIndex = 0, long endIndex = long.MaxValue)
		{
			yield break;
		}

		public IEnumerable<IChat> GetChats()
		{
			return ChatManager.GetChats();
		}

		public IChat GetChat(SourceReference chatId)
		{
			try {
				return ChatManager.GetChat(chatId);
			} catch(KeyNotFoundException){
				throw new ChatNotFoundException(chatId);
			}
		}

		public void AddChat(IChat chat)
		{
			ChatManager.AddChat(chat);
		}
	
		/// <summary>
		/// Saves all chats and messages to disc.
		/// Should be executed when application is closed.
		/// </summary>
		public void Save()
		{
			ChatManager.SaveChats();
			MessageManager.SaveMessages();
		}
	}

	/// <summary>
	/// Thrown when a requested chat wasn't found
	/// </summary>
	public class ChatNotFoundException : Exception
	{
		public ChatNotFoundException() : base("The chat wasn't found")
		{

		}

		public ChatNotFoundException(SourceReference chatId) : base("The chat {chatId} wans't found")
		{

		}
	}


	public interface IChatManager
	{
		IEnumerable<IChat> GetChats();

		IChat GetChat(SourceReference chatId);

		void AddChat(IChat chat);

		bool RemoveChat(IChat chat);

		void SaveChats();
	}

	public interface IMessageManager {
		/// <summary>
		/// Gets messages for a chat, will return at least one message if it exists also if the index doesn't match.
		/// eg. if you set startindex to Long.maxvalue you will get the last message
		/// </summary>
		/// <param name="chat">The chat to get the messages for</param>
		/// <param name="count">The amount of messages to get</param>
		/// <param name="offset">The amount of messages to skip</param>
		/// <returns>Collection of Messages</returns>
		IEnumerable<LocalChatMessage> MessagesForChat(IChat chat, int count = 10, long offset = 0);
	
		void SaveMessage(LocalChatMessage message);

		/// <summary>
		/// Deletes the message
		/// </summary>
		/// <param name="message">the message to delete</param>
		void DeleteMessage(LocalChatMessage message);

		/// <summary>
		/// Saves all messages
		/// </summary>
		void SaveMessages();
	}


    public class CoflnetChatManager : IChatManager
    {
		Dictionary<SourceReference, IChat> chats;

		public CoflnetChatManager()
		{
			LoadChats();
			UnityEngine.Debug.Log("loaded");
		}

		~CoflnetChatManager()
		{
			SaveChats();
			UnityEngine.Debug.Log("saving :D");
		}

		


        public IChat GetChat(SourceReference chatId)
        {
            return chats[chatId];
        }

        public IEnumerable<IChat> GetChats()
        {
			return chats.Values;
        }

		public void LoadChats()
		{
			chats = DataController.Instance.LoadObject<Dictionary<SourceReference,IChat>>("chats",
			()=>new Dictionary<SourceReference, IChat>());
		}

        public void AddChat(IChat chat)
        {
            chats.Add(chat.ID,chat);
        }

		public void SaveChats()
		{
			DataController.Instance.SaveObject("chats",chats);
		}

        public bool RemoveChat(IChat chat)
        {
            return chats.Remove(chat.ID);
        }
    }

	/// <summary>
	/// Stores messages in files
	/// </summary>
    public class CoflnetMessageManager : IMessageManager
    {
        public void DeleteMessage(LocalChatMessage message)
        {
            DataController.Instance.RemoveFromFile<ChatMessage>(
				$"messages/{message.id.Source}",
			(m)=>m.Equals(message));
        }


        public IEnumerable<LocalChatMessage> MessagesForChat(IChat chat, int count = 10, long offset = 0)
        {

			// prevent searching for longer than the chat exists
			// TODO

			var reverseMessageIndex = chat.lastMessageIndex;

			List<LocalChatMessage> result = new List<LocalChatMessage>();

			// we try to find older files as long as we don't have reached the requested count
			while(result.Count < count)
			{
				reverseMessageIndex-=GroupSize;
				foreach (var message in FileController.ReadLinesAs<LocalChatMessage>(FileName(chat.ID,reverseMessageIndex)))
				{
					
					result.Add(message);
					if(result.Count == count+1){
						break;
					}
				}
			}
			// we want the youngest message to be first
			result.RemoveAt(count);
			result.Reverse();
			return result;
        }

        public void SaveMessage(LocalChatMessage message)
        {
            FileController.AppendLineAs<LocalChatMessage>(FileName(message.chat,message.id.IdfromSource),message);
        }

        public void SaveMessages()
        {
            // does nothing, messages are always persisted
        }

        private string FileName(SourceReference chatId,long messageIndex)
		{
			var index = messageIndex /GroupSize;
			return $"messages/{chatId}-{index}";
		}

		private long GroupSize => 86400L*10000*1000;

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



		public LocalChatMessage(string content, DateTime timetamp, MessageReference refs, MessageReference id, Type type, Dictionary<SourceReference, Status> states = null) : base(content, timetamp, refs, id, type)
		{
			States = states;
		}

		public LocalChatMessage()
		{
		}
	}
}

