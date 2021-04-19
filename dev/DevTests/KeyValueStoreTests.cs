using Coflnet.Dev;
using NUnit.Framework;
using Coflnet;
using Core.Extentions.KeyValue;
using System;
using Coflnet.Client;
using System.Threading.Tasks;

public class KeyValueStoreTests
{
    EntityId deviceId = new EntityId(1, 1);

    [Test]
    public async Task DistribedAddTest()
    {
        DevCore.Init(deviceId, testSetup: true);
        var key = "test";
        var value = new EntityId(1, 2);
        var client = DevCore.DevInstance.GetInstance(deviceId) as ClientCore;
        var storeProxy = client.CreateEntity<CreateKeyValueStoreCommand>();
        var store = client.EntityManager.GetEntity<KeyValueStore>(storeProxy.Id);

        var service = DevCore.Instance.GetService<KVService>();
        service.Add(key, value, store.Id);
        var resolved = await service.Resolve(key, store.Id);
        Assert.AreEqual(value, resolved);
    }

    [Test]
    public async Task DistribedMultiBucketTest()
    {
        DevCore.Init(deviceId, testSetup: true);
        Logger.OnLog += Console.WriteLine;
        Logger.OnError += Console.WriteLine;
        var key = "test";
        var value = EntityId.NextLocalId;
        var client = DevCore.DevInstance.GetInstance(deviceId) as ClientCore;
        var storeProxy = client.CreateEntity<CreateKeyValueStoreCommand>();
        var store = client.EntityManager.GetEntity<KeyValueStore>(storeProxy.Id);
        Logger.Log(Newtonsoft.Json.JsonConvert.SerializeObject(storeProxy));
        Logger.Log(Newtonsoft.Json.JsonConvert.SerializeObject(store));
        
        client.SendCommand<AddBucketToStoreCommand>(store.Id);


        var service = DevCore.Instance.GetService<KVService>();
        service.Add(key, value, store.Id);
        var resolved = await service.Resolve(key, store.Id);
        Assert.AreEqual(value, resolved);
    }
}