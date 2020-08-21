using System.Linq;



namespace Coflnet.Core
{
    /// <summary>
    /// Collection of Scopes for some <see cref="Entity"/>
    /// </summary>
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