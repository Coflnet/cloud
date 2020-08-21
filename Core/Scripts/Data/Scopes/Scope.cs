using System;
using System.Collections.Generic;
using Coflnet;



namespace Coflnet.Core
{
    /// <summary>
    /// Scopes are like <see cref="Permission"/> for data
    /// </summary>
    public abstract class Scope : IHasSlug
    {
        private HashSet<string> _commands = new HashSet<string>();

        /// <summary>
        /// Copies fields that this scope gives access to from old to new
        /// </summary>
        /// <param name="newRes">The new object that will be sent out</param>
        /// <param name="oldRes">The stored object containing data</param>
        public virtual void SetFields(IHasScopes newRes, in IHasScopes oldRes)
        {
            
            RequireAs<Entity>(newRes).Id = RequireAs<Entity>(oldRes).Id;
        }

        /// <summary>
        /// Makes sure an object is of a specific type
        /// </summary>
        /// <typeparam name="T">The type to test</typeparam>
        protected T RequireAs<T>(IHasScopes obj) where T:class
        {
            var val = obj as T;
            if(val == null)
            {
                throw new System.Exception($"The given object is not of the required type {typeof(T).Name} but {obj.GetType().Name}");
            }

            return val;
        }


        /// <summary>
        /// Helper for shorter assignments 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected void Set<T>(IHasScopes newRes,in IHasScopes oldRes,Action<T,T> assing) where T:class
        {
            assing.Invoke(RequireAs<T>(newRes),RequireAs<T>(oldRes));
        }
 
        
        public virtual HashSet<string> Commands => _commands;

        /// <summary>
        /// Unique human readable identifier for this Scope. eg "readUserName"
        /// </summary>
        /// <value>By default the name of the class</value>
        public virtual string Slug => this.GetType().Name;
    }

    /// <summary>
    /// This object contains scopes
    /// </summary>
    public interface IHasScopes
    {
        ScopesList AvailableScopes {get;}
    }

}