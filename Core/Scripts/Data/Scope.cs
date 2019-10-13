using System;
using System.Collections.Generic;
using Coflnet;
using System.Linq;



namespace Coflnet.Core
{
    /// <summary>
    /// Scopes are like <see cref="Permission"/> for data
    /// </summary>
    public class Scope : IHasSlug
    {
        private HashSet<string> _commands = new HashSet<string>();

        /// <summary>
        /// Copies fields that this scope gives access to from old to new
        /// </summary>
        /// <param name="newRes">The new object that will be sent out</param>
        /// <param name="oldRes">The stored object containing data</param>
        public virtual void SetFields(IHasScopes newRes, IHasScopes oldRes)
        {
            
            RequireAs<Referenceable>(newRes).Id = RequireAs<Referenceable>(oldRes).Id;
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
        protected void Set<T>(IHasScopes newRes,IHasScopes oldRes,Action<T,T> assing) where T:class
        {
            assing.Invoke(RequireAs<T>(newRes),RequireAs<T>(oldRes));
        }
 
        
        public virtual HashSet<string> Commands => _commands;

        public string Slug
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// This object contains scopes
    /// </summary>
    public interface IHasScopes
    {
        ScopesList AvailableScopes {get;}
    }




    public class ScopesList : ClassBasedDictionary<string,Scope>
    {
        public bool IsAllowedToExecute(string commandSlug)
        {
            // any of the Scopes contain the slug
            return Items.Where(val=>val.Value.Commands.Contains(commandSlug)).Any();
        }

        /// <summary>
        /// Tries to find the commandSlug in any of the grantedScopes.
        /// Returns true if successful and execution can be started.
        /// </summary>
        /// <param name="grantedScopes">Granted scopes, eg from a token</param>
        /// <param name="commandSlug">The commandSlug that is about to be executed</param>
        /// <returns><see cref="true"/> if execution can start</returns>
        public bool IsAllowedToExecute(string[] grantedScopes,string commandSlug)
        {
            // filter out the granted scopes in this scope list
            var activeScopes = grantedScopes.Where(x=>Items.ContainsKey(x)).Select(x=>this[x]);
            // any of the active scopes contain the command?
            return activeScopes.Where(val=>val.Commands.Contains(commandSlug)).Any();
            //return Items.Where(v=>scopes.Contains(v)).Where(val=>val.Value.Commands.Contains(commandSlug)).Any();
        }


        public bool IsAllowedToExecute(string commandSlug,string[] slugs)
        {
            // any of the Scopes contain the slug
            foreach (var item in slugs)
            {
                if(Items.ContainsKey(item) && Items[item].Commands.Contains(commandSlug))
                    return true;
            }
            return false;
        }
    }

}