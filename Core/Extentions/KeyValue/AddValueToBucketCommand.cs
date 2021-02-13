using Coflnet;
using System.Collections.Generic;

namespace Core.Extentions.KeyValue
{
    public class AddValueToBucketCommand : Command
    {
        public override void Execute(CommandData data)
        {
            var bucket= data.GetTargetAs<KeyValueBucket>();
            var pair = data.GetAs<KeyValuePair<string,EntityId>>();
            bucket.Values.AddOrUpdate(pair.Key,pair.Value,(key,value)=>value);
        }
    }



}