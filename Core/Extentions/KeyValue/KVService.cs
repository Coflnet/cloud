using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Coflnet;

namespace Core.Extentions.KeyValue
{
    /// <summary>
    /// abstraction for Key-Value-Resolution
    /// </summary>
    public class KVService
    {
        private CoflnetCore coreInstance;

        public KVService(CoflnetCore coreInstance)
        {
            this.coreInstance = coreInstance;
        }


        /// <summary>
        /// Adds a new Key and Value to a KeyValueStore
        /// </summary>
        /// <param name="key">The key to add a value for</param>
        /// <param name="id">The EntityId to add</param>
        /// <param name="kvId">(optional) different KeyValueStore from the default one</param>
        public void Add(string key, EntityId id, EntityId kvId = default(EntityId))
        {
            var pair = new KeyValuePair<string, EntityId>(key, id);
            coreInstance.SendCommand<AddValueToStoreCommand, KeyValuePair<string, EntityId>>(kvId, pair);
        }

        /// <summary>
        /// Resolves a key to an <see cref="EntityId"/>
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <param name="kvId">(optional) <see cref="KeyValueStore"/> to search the values in</param>
        /// <returns>An <see cref="EntityId"/> for the key or default(EntityId)</returns>
        public Task<EntityId> Resolve(string key, EntityId kvId = default(EntityId))
        {
            var task = new TaskCompletionSource<EntityId>();
            coreInstance.SendGetCommand <GetValueFromStoreCommand,string>(kvId,key,data=>
            {
                var result = data.GetAs<EntityId>();
                task.TrySetResult(result);

            });

            return task.Task;
        }
    }

    public class GetValueFromStoreCommand : GetCommand<EntityId>
    {
        public async override Task<EntityId> GetObjectAsync(CommandData data)
        {
            var store = data.GetTargetAs<KeyValueStore>();
            var key = data.GetAs<string>();
            var bucketId = store.GetBucketId(key);
            var response = await data.SendGetCommand<GetValueFromBucketCommand,string,EntityId>(bucketId,key);
            return response;
        }
    }

    public class GetValueFromBucketCommand : GetCommand<EntityId>
    {
        public override EntityId GetObject(CommandData data)
        {
            var bucket = data.GetTargetAs<KeyValueBucket>();
            var value = bucket.Values[data.GetAs<string>()];
            return value;
        }
    }

}