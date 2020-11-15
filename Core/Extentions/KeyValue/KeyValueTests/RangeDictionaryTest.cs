using Coflnet;
using NUnit.Framework;

namespace Core.Extentions.KeyValue.Tests
{
    public class RangeDictionaryTest
    {
        [Test]
        public void Add()
        {
        //    var dictionary = new RangeDictionary<string>();
        //    dictionary.Add(0,3,"first");
        //    Assert.AreEqual("first",dictionary[2]);
        }
    }

    public class KVServiceTest
    {
        KVService kv = new KVService();
        EntityId id = new EntityId(12,3456789);
        EntityId kvId = new EntityId(12,9874552);
        string key = "key";

        [Test]
        public void Add()
        {
            kv.Add(key,id,kvId);
        }


        [Test]
        public void Resolve()
        {
            var id = kv.Resolve("key",kvId);
            Assert.AreEqual(this.id,id);
        }
    }
}