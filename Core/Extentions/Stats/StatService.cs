using System;
using System.Collections.Concurrent;

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
}