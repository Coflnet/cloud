using Coflnet;

namespace Core.Extentions.KeyValue
{
    public class CreateKeyValueStoreCommand : CreationCommand
    {
        public override Entity CreateResource(CommandData data)
        {
            var store = new KeyValueStore();
            var firstBucket = new KeyValueBucket();
            firstBucket.AssignId(data.CoreInstance.EntityManager);
            store.AddBucket(firstBucket);
            return store;
        }
    }



}