using Coflnet;
using System.Collections.Generic;

namespace Core.Extentions.KeyValue
{
    public class AddValueToStoreCommand : Command
    {
        public override void Execute(CommandData data)
        {
            var store = data.GetTargetAs<KeyValueStore>();
            var args = data.GetAs<KeyValuePair<string,EntityId>>();
            var bucketId = store.GetBucketId(args.Key);
            data.SendCommandTo<AddValueToBucketCommand,KeyValuePair<string,EntityId>>(bucketId,args);
        }
    }



}