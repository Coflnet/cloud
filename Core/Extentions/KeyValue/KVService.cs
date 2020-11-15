using System;
using Coflnet;

namespace Core.Extentions.KeyValue
{
    /// <summary>
    /// abstraction for Key-Value-Resolution
    /// </summary>
    public class KVService
    {
        /// <summary>
        /// Adds a new Key and Value to a KeyValueStore
        /// </summary>
        /// <param name="key">The key to add a value for</param>
        /// <param name="id">The EntityId to add</param>
        /// <param name="kvId">(optional) different KeyValueStore from the default one</param>
        public void Add(string key, EntityId id, EntityId kvId = default(EntityId))
        {

        }

        /// <summary>
        /// Resolves a key to an <see cref="EntityId"/>
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <param name="kvId">(optional) <see cref="KeyValueStore"/> to search the values in</param>
        /// <returns>An <see cref="EntityId"/> for the key or default(EntityId)</returns>
        public EntityId Resolve(string key, EntityId kvId = default(EntityId))
        {
            throw new NotImplementedException();
        }
    }

}