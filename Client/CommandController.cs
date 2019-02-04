using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessagePack;

namespace Coflnet.Client
{



	/// <summary>
	/// Identifys some Resource, from some resource, from some server.
	/// eg a chat message from a user on a specific server.
	/// </summary>
	[MessagePackObject]
	public struct MessageReference
	{
		private SourceReference source;
		private long idfromSource;

		[SerializationConstructor]
		public MessageReference(SourceReference source, long idfromSource)
		{
			this.source = source;
			this.idfromSource = idfromSource;
		}

		[Key(0)]
		public SourceReference Source
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

		[IgnoreMember]
		public static MessageReference Next
		{
			get
			{
				return new MessageReference(
					ConfigController.UserSettings.userId,
					ThreadSaveIdGenerator.NextId);
			}
		}
	}

}

