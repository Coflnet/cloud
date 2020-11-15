using System;
using System.Collections.Generic;

namespace Core.Extentions.KeyValue.Tests
{
    internal class RangeDictionary<T>
    {
        public List<Node<T>> Buckets;

        public RangeDictionary()
        {
        }

        internal void Add(int from, int until, T value)
        {
            // Todo
        }

        public T this[int key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        private void SetValue(int key, T value)
        {
            throw new NotImplementedException();
        }

        private T GetValue(int key)
        {
            return default(T);
        }
    }

    public class Node<TVal>
    {
        public int Lower {get;set;}
        public int Higher {get;set;}

        public TVal Value {get;set;}
    }
}