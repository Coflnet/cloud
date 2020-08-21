using System.Collections;
using System.Collections.Generic;
using MessagePack;
using System;

namespace Coflnet
{



	/// <summary>
	/// Identifys some Resource, from some resource, from some server.
	/// eg a chat message from a user on a specific server.
	/// </summary>
	[MessagePackObject]
	public struct MessageReference
	{
		private EntityId source;
		private long idfromSource;

		[SerializationConstructor]
		public MessageReference(EntityId source, long idfromSource)
		{
			this.source = source;
			this.idfromSource = idfromSource;
		}

		[Key(0)]
		public EntityId Source
		{
			get
			{
				return source;
			}
		}

		[Key(1)]
		public long IdfromSource
		{
			get
			{
				return idfromSource;
			}
		}

		/// <summary>
		/// Generates new unique <see cref="MessageReference"/>
		/// </summary>
		/// <value></value>
		[IgnoreMember]
		public static MessageReference Next
		{
			get
			{
				return new MessageReference(
					ConfigController.ActiveUserId,
					ThreadSaveIdGenerator.NextId);
			}
		}

		/// <summary>
		/// Determines if the id is equal to this one
		/// </summary>
		/// <param name="obj">The id to compare to</param>
		/// <returns>true if it is equal falso otherwise</returns>
		public override bool Equals(object obj)
		{			
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}
			
			var reference = (MessageReference)obj;

			return reference.source == this.source 
			&& reference.idfromSource == this.idfromSource;
		}
		
		// override object.GetHashCode
		public override int GetHashCode()
		{
			return (idfromSource ^ source.GetHashCode()).GetHashCode();
		}	

		public override string ToString()
		{
			return $"{source.ToString()}-{Convert.ToString(idfromSource, 16)}";
		}
	}

}

