using Coflnet.Dev;
using NUnit.Framework;
using Coflnet;
using Core.Extentions.KeyValue;

public class KeyValueStoreTests
{
    [Test]
    public void DistribedAddTest()
    {
        DevCore.Init(new EntityId(1,1));
        var service = new KVService(DevCore.Instance);
        var manager = DevCore.DevInstance.GetInstance(new EntityId(1, 0)).EntityManager;
        var store = new KeyValueStore();
        store.AssignId(manager);
        DevCore.Instance.Id = new EntityId(55, 23);

        service.Add("test", new EntityId(1, 2), store.Id);
        
    }
}