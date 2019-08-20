using System;
using System.Collections;
using System.Collections.Generic;

namespace Coflnet.Core
{
    public class RemoteDictionary<TKey, TValue> : RemoteObject<Dictionary<TKey, TValue>>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
    {
        public TValue this[TKey key] 
        { 
            get 
            {
                return Value[key];
            } 
            set 
            {
                Send("set",new KeyValuePair<TKey,TValue>(key,value));
            } 
        }

        public int Count => Value.Count;

        public bool IsReadOnly => false;

        public ICollection<TKey> Keys => Value.Keys;

        public ICollection<TValue> Values => Value.Values;

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Send("add",item);
        }

        public void Add(TKey key, TValue value)
        {
            Send("add",new KeyValuePair<TKey, TValue>(key,value));
        }

        public void Clear()
        {
            Send("clear");
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            //return Value.Contains(item);
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey item)
        {
            return Value.ContainsKey(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {

            throw new NotImplementedException();

           // Value.CopyTo(array,arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Value.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            Send("remove",key);
			return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Value.TryGetValue(key,out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Value.GetEnumerator();
        }
    }


}