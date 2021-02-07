using System.Collections.Generic;
using Coflnet;
using NUnit.Framework;
using System.Linq;

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
        KVService kv = new KVService(new DummyCore());
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
            kv.Add(key,this.id,kvId);
            var id = kv.Resolve("key",kvId);
            Assert.AreEqual(this.id,id);
        }

        [Test]
        public void Test()
        {
            var list = new SortedList<int,int>();
            list.Add(5,9);
            list.Add(10,19);
            list.Add(20,29);
            list.Add(22,29);

            //throw new System.Exception(list.Keys.ToList().BinarySearch(21).ToString());
        }

        [Test]
        public void HashTest()
        {
            var store = new KeyValueStore();
            Assert.AreNotEqual(store.GetHash("d"),store.GetHash("b"));
            Assert.AreNotEqual(store.GetHash("x"),store.GetHash("b"));
            Assert.AreEqual(store.GetHash("b"),store.GetHash("b"));
        }



        
    }
}