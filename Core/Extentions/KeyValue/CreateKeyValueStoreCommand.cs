using Coflnet;

namespace Core.Extentions.KeyValue
{
    public class CreateKeyValueStoreCommand : CreationCommand
    {
        public override Entity CreateEntity(CommandData data)
        {
            var store = new KeyValueStore();
            return store;
        }

        protected override void AfterIdAssigned(CommandData data, Entity entity)
        {
            var store = entity as KeyValueStore;
            
            var firstBucket = new KeyValueBucket();
            firstBucket.AssignId(data.CoreInstance.EntityManager);
            store.AddBucket(firstBucket);
            firstBucket.GetAccess().Owner = entity.Id;
        }
    }


    public class AddBucketToStoreCommand : Command
    {
        public override void Execute(CommandData data)
        {
            // lock write to old bucket
            // create new bucket
            // tell old bucket to send key range to new bucket
            // wait for new bucket to confirm receival
            // adopt store index and release lock on old bucket
            // any write attempts to locked buckets are queued 
        }
    }
}