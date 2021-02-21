using Coflnet.Dev;
using NUnit.Framework;
using Coflnet;
using Core.Extentions.KeyValue;
using System;
using Coflnet.Client;
using System.Threading.Tasks;

public class KeyValueStoreTests
{
    [Test]
    public async Task DistribedAddTest()
    {
        var deviceId = new EntityId(1,1);
        DevCore.Init(deviceId,testSetup:true);
        var serverId = new EntityId(1,0);
        Logger.OnLog += Console.WriteLine;
        Logger.OnError += Console.WriteLine;
        var key = "test";
        var value = new EntityId(1, 2);
        var service = new KVService(DevCore.Instance);
        var server = DevCore.DevInstance.GetInstance(new EntityId(1, 0));
        var client = DevCore.DevInstance.GetInstance(new EntityId(1, 1)) as ClientCore;
        var storeProxy = client.CreateEntity<CreateKeyValueStoreCommand>();
        var store = client.EntityManager.GetEntity<KeyValueStore>(storeProxy.Id);
        Logger.Log(Newtonsoft.Json.JsonConvert.SerializeObject(store));
        /*
        var store = new KeyValueStore();
        store.AssignId(manager);
        store.GetAccess().generalAccess = Access.GeneralAccess.ALL_READ_AND_WRITE;
        var bucket = new KeyValueBucket();
        bucket.AssignId(manager);
        store.AddBucket(bucket);*/
       
        //DevCore.Instance.Id = new EntityId(55, 23);


        service.Add(key, value, store.Id);
        var resolved = await service.Resolve(key,store.Id);
        Assert.AreEqual(value,resolved);
    }
}