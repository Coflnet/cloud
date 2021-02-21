using Coflnet;
using MessagePack;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Extentions.KeyValue
{
    /// <summary>
    /// Provides a lookup of induvidual Buckets containing the actual KV-Pairs 
    /// Needed in order to find the Buckets in the distributed cloud
    /// </summary>
    public class KeyValueStore : Entity
    {
        //public RangeTree<int,EntityId> Buckets {get;set;}
        public SortedList<ushort, Entry> Buckets { get; set; }
        private List<ushort> Lookup = new List<ushort>();
        private static CommandController Commands;

        static KeyValueStore()
        {
            Commands = new CommandController(globalCommands);
            Commands.RegisterCommand<AddValueToStoreCommand>();
            Commands.RegisterCommand<GetValueFromStoreCommand>();
        }

        public KeyValueStore()
        {
            Buckets = new SortedList<ushort, Entry>();
        }


        public override CommandController GetCommandController()
        {
            return Commands;
        }

        public EntityId GetBucketId(string key)
        {
            var bucketLookup = GetHash(key);
            var index = Lookup.BinarySearch(bucketLookup);
            if(index < 0)
                index = ~index;
            Logger.Log(index);
            var bucketId = Buckets.Values[index];
            return bucketId.Bucket;
        }

        public ushort GetHash(string key)
        {
            return (ushort)CalculateHash(key);
        }

        /// <summary>
        /// From David Schwarz https://stackoverflow.com/a/9545731
        /// </summary>
        /// <param name="read"></param>
        /// <returns></returns>
        static ulong CalculateHash(string read)
        {
            ulong hashedValue = 3074457345618258791ul;
            for (int i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        public class Entry
        {
            public ushort Min { get; set; }
            public ushort Max { get; set; }
            public EntityId Bucket { get; set; }
        }

        public void AddBucket(KeyValueBucket keyValueBucket)
        {
            var entry = new Entry()
            {
                Bucket = keyValueBucket.Id,
                Max = ushort.MaxValue,
                Min = ushort.MinValue
            };
            Buckets.Add(ushort.MaxValue,entry);
            Lookup.Add(ushort.MaxValue);
        }
    }



}