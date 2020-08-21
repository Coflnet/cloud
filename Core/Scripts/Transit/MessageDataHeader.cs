using MessagePack;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet
{
    public class CommandDataHeader : Dictionary<int,byte[]>
	{
		public enum Type
		{
			Authorization = 1,
			Upgrade = 2,
			Expires = 3,
			Accept_Encoding = 4,
			Accept = 5,
			DNT = 6,
			Redirect = 7,
			/// <summary>
			/// The next public key for the signal Diffie-Hellman ratchet root  chain step
			/// </summary>
			SignalRatchetPK = 8,

			/// <summary>
			/// The count of messages for the previous block
			/// </summary>
			SignalPreviousN = 9,
			SingalN = 10,
			/// <summary>
			/// The setup information needed to establish a new signal channel
			/// </summary>
			SignalSetup = 11,
			/// <summary>
			/// Payload or payload options, unencrypted
			/// </summary>
			PlainPayload = 12


		}

		/// <summary>
		/// Sets a specific header type to some value
		/// </summary>
		/// <param name="type">The header type to set</param>
		/// <param name="value">The value to set the header to</param>
		/// <typeparam name="T">Type of the value</typeparam>
		public void SetHeader<T>(Type type, T value)
		{
			this[(int)type] = MessagePackSerializer.Serialize<T>(value);
		}

		/// <summary>
		/// Returns the Header content as some object
		/// </summary>
		/// <param name="type">The header type</param>
		/// <typeparam name="T">The type to deserialize to</typeparam>
		/// <returns>Deserialized Header Data</returns>
		public T GetHeaderAs<T>(Type type)
		{
			if(!ContainsKey((int)type)){
				throw new HeaderNotFoundException($"The Header {type} is missing");
			}
			return MessagePackSerializer.Deserialize<T>(this[(int)type]);
		}

		public byte[] Serialized
		{
			get {
				return MessagePackSerializer.Serialize(this);
			}
		}


		/// <summary>
		/// Gets or sets a <see cref="Type.Authorization"/> token header
		/// </summary>
		/// <value>The <see cref="Type.Authorization"/> token</value>
		public Token Token
		{
			get 
			{
				return GetHeaderAs<Token>(Type.Authorization);
			}
			set 
			{
				SetHeader(Type.Authorization,value);
			}
		}

		

		/// <summary>
		/// Header field doesn't exist on this header
		/// </summary>
        class HeaderNotFoundException : KeyNotFoundException
        {
            public HeaderNotFoundException()
            {
            }

            public HeaderNotFoundException(string message) : base(message)
            {
            }

            public HeaderNotFoundException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected HeaderNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}


