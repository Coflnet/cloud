using System;
using MessagePack;

namespace Coflnet.Client.Messaging
{
    /// <summary>
    /// A message inside a chat
    /// </summary>
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


		public ChatMessage(string content, DateTime timetamp, MessageReference refs, MessageReference id, Type type)
		{
			this.content = content;
			this.timetamp = timetamp;
			this.refs = refs;
			this.id = id;
			this.type = type;
		}

		public ChatMessage(string content) : this(content, DateTime.Now, default(MessageReference), MessageReference.Next, Type.Message)
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

		// override object.Equals
		public override bool Equals(object obj)
		{
			var objc = obj as ChatMessage;

			if (objc == null )
			{
				return false;
			}
			
			return objc.id.Equals(this.id);
		}
		
		// override object.GetHashCode
		public override int GetHashCode()
		{
			// TODO: write your implementation of GetHashCode() here
			throw new System.NotImplementedException();
			return base.GetHashCode();
		}
	}
}

