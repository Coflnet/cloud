using Coflnet;
using RangeTree;

namespace Core.Extentions.KeyValue
{
    /// <summary>
    /// Provides a lookup of induvidual Buckets containing the actual KV-Pairs 
    /// Needed in order to find the Buckets in the distributed cloud
    /// </summary>
    public class KeyValueStore : Entity
    {
        public RangeTree<int,EntityId> Buckets {get;set;}

        public KeyValueStore()
        {
            Buckets = new RangeTree<int, EntityId>();
        }


        public override CommandController GetCommandController()
        {
            throw new System.NotImplementedException();
        }


    }


}