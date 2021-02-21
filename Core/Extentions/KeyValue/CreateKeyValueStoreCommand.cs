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



}