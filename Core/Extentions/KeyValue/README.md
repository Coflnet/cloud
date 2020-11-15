The KeyValue-Extension provides Id-Resolution.

## Concept
The `KeyValueStoreEntity` Resolves to `BucketEntities` containing the KeyValuePairs.
```
         KVStore
        /       \
     Bucket    Bucket
    /     \    /     \
  Pair  Pair  Pair   Pair
```
This archives very high scalability through sharding.

## FAQ
### How do I find the KeyValueStore itself
You DNS query the root server `kv.cloud.coflnet.com`, open a wss socket and call the command `getRootKV`
### Why not just DNS
DNS resolution default behavior is cache response. This leads to out of date entries, wich should be controlled by the cloud itself.