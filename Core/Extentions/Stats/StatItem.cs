using System;
using MessagePack;
using System.Collections.Generic;

namespace Coflnet.Core.Extention
{
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