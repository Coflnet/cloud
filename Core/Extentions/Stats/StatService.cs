using System;
using MessagePack;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Coflnet.Core.Extention
{
	public class StatService
	{
		public static StatService Instance;

		public CoflnetCore coreInstance;

		public ConcurrentBag<LogItem> Log;
		public ConcurrentBag<LogItem> Processed;

		public ConcurrentDictionary<long,StatItem> Stats;

		static StatService()
		{
			Instance = new StatService();
		}

		public void Executed(EntityId target, string type, EntityId sender)
		{
			var logEntry = new LogItem(
							sender,
							target,
							DateTime.Now.ToFileTimeUtc(),
							type);

			Log.Add(logEntry);
			
		}

		public void Process()
		{
			while(!Log.IsEmpty)
			{
				LogItem logEntry;
				Log.TryTake(out logEntry);
				// null could have been inserted
				if(logEntry != null)
				{
					// key depends on the type and target
					var key = logEntry.type.GetHashCode() * logEntry.target.GetHashCode();
					Stats.AddOrUpdate(key,k=>{
						return new StatItem(logEntry.target,1,logEntry.type,coreInstance.Id,logEntry.TimeStamp,logEntry.TimeStamp);
					},(k,v)=>{
						v.count++;
						v.endTime=logEntry.TimeStamp;
						return v;
					});

					// save it for later
					Processed.Add(logEntry);
				}
			}
		}

		/// <summary>
		/// Saves all Processed Stats to disc
		/// </summary>
		public void Save()
		{
			var oldStats = Stats;
			var oldProcessed = Processed;
			// replace the old period with new one
			Stats = new ConcurrentDictionary<long, StatItem>();
			Processed = new ConcurrentBag<LogItem>();

			// and save it to disc if it contains any data
			if(oldStats.Count != 0)
				DataController.Instance.SaveObject($"stats/{DateTime.Now.ToFileTimeUtc()}",oldStats);
			if(oldProcessed.Count != 0)
				DataController.Instance.SaveObject($"log/{DateTime.Now.ToFileTimeUtc()}",oldProcessed);
		}
	}

	[MessagePackObject]
	public class LogItem
	{
		[Key(0)]
		public EntityId sender;
		[Key(1)]
		public EntityId target;
		[Key(2)]
		public long TimeStamp;
		[Key(3)]
		public string type;

        public LogItem(EntityId sender, EntityId target, long timeStamp, string type)
        {
            this.sender = sender;
            this.target = target;
            TimeStamp = timeStamp;
            this.type = type;
        }
    }

	[MessagePackObject]
	public class StatItem
	{
		[Key(0)]
		public EntityId targetId;

		[Key(1)]
		public int count;

		[Key(2)]
		public string type;
		
		[Key(3)]
		public EntityId serverId;		
		[Key(4)]
		public long startTime;
		[Key(5)]
		public long endTime;

        public StatItem(EntityId targetId, int count, string type, EntityId serverId, long startTime, long endTime)
        {
            this.targetId = targetId;
            this.count = count;
            this.type = type;
            this.serverId = serverId;
            this.startTime = startTime;
            this.endTime = endTime;
        }

        public override bool Equals(object obj)
        {
            var item = obj as StatItem;
            return item != null &&
                   EqualityComparer<EntityId>.Default.Equals(targetId, item.targetId) &&
                   type == item.type;
        }

        public override int GetHashCode()
        {
            var hashCode = -836728082;
            hashCode = hashCode * -1521134295 + EqualityComparer<EntityId>.Default.GetHashCode(targetId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(type);
            return hashCode;
        }
    }
}