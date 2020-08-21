using System;
using MessagePack;
using Coflnet.Client;
using System.Collections.Generic;
using System.Linq;

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

			DataController.Instance.RegisterSaveCallback(Save);
		}

		static ChatService()
		{
			Instance = new ChatService();


		}

		public event Action<ChatMessage> OnReceiveMessage;

		public void ReceiveMessage(ChatMessage message,EntityId chatId)
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

		/// <summary>
		/// Adds a message to the chat.
		/// If the chat parameter is null we try to load the chat from the chatManager.
		/// </summary>
		/// <param name="message">The message to save</param>
		/// <param name="chat">Optional the chat to save it to</param>
		public void AddMessage(LocalChatMessage message, IChat chat = null)
		{
			if(chat == null){
				chat = GetChat(message.chatId);
			}
			if(chat.lastMessageIndex<message.id.IdfromSource){
				chat.lastMessageIndex = message.id.IdfromSource;
			}


			message.LocalMessageChatIndex = chat.MessageCount;
			
			chat.MessageCount++;
			MessageManager.SaveMessage(message);
		}

		public void SendMessage(ChatMessage message, EntityId target)
		{
			CoflnetCore.Instance.SendCommand<ChatMessageCommand,ChatMessage>(target,message);
			CoflnetCore.Instance.SendCommand<ChatMessageCommand,ChatMessage>(target,message);
		}


		public IChat CreatePivateChat(EntityId partner)
		{
			return new Chat(new ChatMember(partner));
		}

		public IChat CreateGroupChat(string name,params EntityId[] partners)
		{
			var options = new CreateGroupChat.Params(partners.ToList(),name);
			var chatResource = ClientCore.ClientInstance
								.CreateEntity<CreateGroupChat,CreateGroupChat.Params>(options);
			
			// return the chat with temporary local id
			return new GroupChat(chatResource.Id);
		}



		public ChatMessage GetMessage(MessageReference reference)
		{
			return new ChatMessage();
		}

		public IEnumerable<ChatMessage> GetMessages(IChat chat, int count = 10, long offset = 0)
		{
			return MessageManager.MessagesForChat(chat,count,offset);
		}

		public IEnumerable<IChat> GetChats()
		{
			return ChatManager.GetChats();
		}

		public IChat GetChat(EntityId chatId)
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
		public void Save(DataController dc)
		{
			ChatManager.SaveChats();
			MessageManager.SaveMessages();
		}


		/// <summary>
		/// Deletes all information of chats and messages
		/// </summary>
		public void DeleteAll()
		{
			foreach (var item in ChatManager.GetChats())
			{
				ChatManager.RemoveChat(item);
			}
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

		public ChatNotFoundException(EntityId chatId) : base($"The chat {chatId} wans't found")
		{

		}
	}


	public interface IChatManager
	{
		IEnumerable<IChat> GetChats();

		IChat GetChat(EntityId chatId);

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
		Dictionary<EntityId, IChat> chats;

		public CoflnetChatManager()
		{
			LoadChats();
					}

		~CoflnetChatManager()
		{
			SaveChats();
					}

		


        public IChat GetChat(EntityId chatId)
        {
            return chats[chatId];
        }

        public IEnumerable<IChat> GetChats()
        {
			return chats.Values;
        }

		public void LoadChats()
		{
			chats = DataController.Instance.LoadObject<Dictionary<EntityId,IChat>>("chats",
			()=>new Dictionary<EntityId, IChat>());
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
			if(chat.MessageCount < count){
				count = (int)chat.MessageCount;
			}

			var reverseMessageIndex = chat.lastMessageIndex;


			var result = new List<LocalChatMessage>(count/2);
			

			// don't search more than 10k files
			var cap = 1000;


			// we try to find older files as long as we don't have reached the requested count
			while(result.Count < count && reverseMessageIndex >0 && cap > 0)
			{
				var fileName = FileName(chat.ID,reverseMessageIndex);
								
				var fileContent = new List<LocalChatMessage>();

				foreach (var message in FileController.ReadLinesAs<LocalChatMessage>(fileName))
				{
					// skip this file if the offset is to high
					if(message.LocalMessageChatIndex >= chat.MessageCount-offset)
					{
						break;
					}

					fileContent.Add(message);
				}


				// add them in the right order 
				// (the last ones after this file, since files are read backwards)
				fileContent.AddRange(result);
				// replace the old one
				result = fileContent;

				// to the next group
				reverseMessageIndex-=GroupSize;
				cap--;
			}
			// we want the youngest message to be first
			result.Reverse();
			return result.Take(count);
        }

        public void SaveMessage(LocalChatMessage message)
        {
            FileController.AppendLineAs<LocalChatMessage>(FileName(message.chatId,message.id.IdfromSource),message);
        }

        public void SaveMessages()
        {
            // does nothing, messages are always persisted
        }

        private string FileName(EntityId chatId,long messageIndex)
		{
			var index = messageIndex /GroupSize;
			return $"messages/{UserService.Instance.CurrentUserId}{chatId}-{index}";
		}
    
		private readonly static long _TICKINADAY = 86400L*10000000;
		
		
		private long GroupSize => _TICKINADAY*1;

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
		Dictionary<EntityId, Status> States;

		/// <summary>
		/// Wherether or not the message as been sent
		/// </summary>
		[Key(11)]
		public bool Sent;

		/// <summary>
		/// The chat id this message coresponds to.
		/// </summary>
		[Key(12)]
		public EntityId chatId;


		/// <summary>
		/// Local counter equal to <see cref="IChat.MessageCount"/> at the time the message is received or sent
		/// Used to find messages faster locally
		/// </summary>
		[Key(13)]
		public long LocalMessageChatIndex;



		public LocalChatMessage(string content, DateTime timetamp, MessageReference refs, MessageReference id, Type type, Dictionary<EntityId, Status> states = null) : base(content, timetamp, refs, id, type)
		{
			States = states;
		}

		public LocalChatMessage(LocalChatMessage message) : this(message.content,message.timetamp,message.refs,message.id,message.type,message.States)
		{

		}

		public LocalChatMessage()
		{
		}

		/// <summary>
		/// Matches the id of the message
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			var message = obj as LocalChatMessage;
			if(message == null){
				return false;
			}

			return message.id.Equals( this.id);
		}

		public override string ToString()
		{
			return $"Message {id}({LocalMessageChatIndex}) '{content}' type {type}";
		}
	}
}

