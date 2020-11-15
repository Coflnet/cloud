using MessagePack;

namespace Coflnet.Core.Extention
{
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
}