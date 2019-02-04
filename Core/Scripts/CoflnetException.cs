using System;
using MessagePack;


namespace Coflnet
{
	public class CoflnetException : Exception
	{
		public string Slug;
		public string UserMessage;
		public int ResponseCode;
		public string Info;
		public long MsgId;

		public CoflnetException(string slug, string message, string userMessage = null, int responseCode = 0, string info = null, long msgId = -1) : base(message)
		{
			Slug = slug;
			UserMessage = userMessage;
			ResponseCode = responseCode;
			Info = info;
			MsgId = msgId;
		}

		public CoflnetException(CoflnetExceptionTransmit transmit) : this(transmit.Slug, transmit.Message, transmit.UserMessage, transmit.ResponseCode, transmit.Info, transmit.MsgId)
		{

		}
	}

	[MessagePackObject]
	public class CoflnetExceptionTransmit
	{
		[Key("slug")]
		public string Slug;
		[Key("message")]
		public string Message;
		[Key("userMessage")]
		public string UserMessage;
		[Key("responseCode")]
		public int ResponseCode;
		[Key("info")]
		public string Info;
		[Key("msgId")]
		public long MsgId;
		[Key("t")]
		public string type = "error";

		public CoflnetExceptionTransmit(string slug, string message, string userMessage = null, int responseCode = 0, string info = null, long msgId = -1)
		{
			Slug = slug;
			Message = message;
			UserMessage = userMessage;
			ResponseCode = responseCode;
			Info = info;
			MsgId = msgId;
		}

		public CoflnetExceptionTransmit(CoflnetException exception)
		{
			Slug = exception.Slug;
			Message = exception.Message;
			UserMessage = exception.UserMessage;
			ResponseCode = exception.ResponseCode;
			Info = exception.Info;
			MsgId = exception.MsgId;
		}
	}
}
