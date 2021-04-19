using System;
using System.Collections.Generic;

namespace Coflnet
{
    /// <summary>
    /// Class Wrapper Around Dictionary that only allows typed values 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TVal"></typeparam>
    public class ClassBasedDictionary<TKey,TVal> 
	{
		protected Dictionary<TKey,TVal> Items = new Dictionary<TKey, TVal>();


		/// <summary>
		/// Overwrites the Item under the given key with an instance of the Generic argument.
		/// </summary>
		/// <param name="key">The key wich to overwrite</param>
		/// <typeparam name="T">The type of <see cref="TVal"/> to overwrite the key with</typeparam>
		public void Overwrite<T>(TKey key) where T : TVal, new()
		{
			Items[key] = (T) Activator.CreateInstance (typeof (T));
		}

		/// <summary>
		/// Generates and adds a item under key
		/// </summary>
		/// <param name="key">The key to add the value for</param>
		/// <typeparam name="T">The type of the class to instantiate and add</typeparam>
		public void Add<T>(TKey key) where T : TVal, new()
		{
			Items.Add(key,(T) Activator.CreateInstance (typeof (T)));
		}

		public void Add<T>() where T:TVal,IHasSlug<TKey>,new()
		{
			var instance = (T) Activator.CreateInstance (typeof (T));
			Items.Add(instance.Slug,instance);
		}


		public TVal this[TKey key]
		{
			get { return Items[key]; }
			protected set { Items[key] = value; }
		}
	}

}