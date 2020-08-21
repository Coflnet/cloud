using System;
using System.Collections;
using System.Collections.Generic;

namespace Coflnet.Core
{
    /// <summary>
    /// <see cref="RemoteObject{T}"/> capeable of updating key-value pairs and generating update command to do so
    /// </summary>
    /// <typeparam name="TKey">The type of the keys</typeparam>
    /// <typeparam name="TValue">The type of the values</typeparam>
    public class RemoteDictionary<TKey, TValue> : RemoteObject<IDictionary<TKey, TValue>>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
    {
        public RemoteDictionary()
        {
        }

        public RemoteDictionary(RemoteObject<IDictionary<TKey, TValue>> remoteObject) : base(remoteObject)
        {
        }

        public RemoteDictionary(string nameOfAttribute, Entity parent) : base(nameOfAttribute, parent)
        {
        }

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
            return Value.Contains(item);
            //throw new NotImplementedException();
        }

        public bool ContainsKey(TKey item)
        {
            return Value.ContainsKey(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {

            //throw new NotImplementedException();

            Value.CopyTo(array,arrayIndex);
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



        public static new void AddCommands(CommandController controller,string nameOfAttribute, Func<CommandData,IDictionary<TKey, TValue>> getter,Action<CommandData,IDictionary<TKey, TValue>> setter)
        {
            RemoteObject<IDictionary<TKey, TValue>>.AddCommands(controller,nameOfAttribute,getter,setter);

        }


    }


    public class RemoteCollectionClearCommand<TContent> : RemoteChangeCommandBase<ICollection<TContent>>
    {
        /// <summary>
        /// Creates a new Instance of the <see cref="RemoteCollectionClearCommand"/> class.
        /// </summary>
        /// <param name="nameOfAttribute">nameof() the attribute the getter will return</param>
        /// <param name="getter">A function to get the collection</param>
        /// <param name="applyLocal">wherether or not this command should be applied locally</param>
        public RemoteCollectionClearCommand(string nameOfAttribute, Func<CommandData, ICollection<TContent>> getter, bool applyLocal = false) 
        : base("clear"+nameOfAttribute, getter, applyLocal)
        {
        }


        /// <summary>
        /// Execute the command logic with specified data.
        /// </summary>
        /// <param name="data"><see cref="CommandData"/> passed over the network .</param>
        public override void Execute(CommandData data)
        {
            getter.Invoke(data).Clear();
        }
    }


    public class RemoteCollectionAddCommand<TContent> : RemoteChangeCommandBase<ICollection<TContent>>
    {
        /// <summary>
        /// Creates a new Instance of the <see cref="RemoteCollectionClearCommand"/> class.
        /// </summary>
        /// <param name="nameOfAttribute">nameof() the attribute the getter will return</param>
        /// <param name="getter">A function to get the collection</param>
        /// <param name="applyLocal">wherether or not this command should be applied locally</param>
        public RemoteCollectionAddCommand(string nameOfAttribute, Func<CommandData, ICollection<TContent>> getter, bool applyLocal = false) 
        : base("add"+nameOfAttribute, getter, applyLocal)
        {
        }


        /// <summary>
        /// Execute the command logic with specified data.
        /// </summary>
        /// <param name="data"><see cref="CommandData"/> passed over the network .</param>
        public override void Execute(CommandData data)
        {
            getter.Invoke(data).Add(data.GetAs<TContent>());
        }
    }


}