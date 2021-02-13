using System.Collections.Concurrent;
using Coflnet;

namespace Core.Extentions.KeyValue
{
    /// <summary>
    /// Single fragment of a <see cref="KeyValueStore"/>
    /// </summary>
    public class KeyValueBucket : Entity
    {
        public ConcurrentDictionary<string,EntityId> Values {get;set;}

        public static CommandController Commands = new CommandController();

        static KeyValueBucket()
        {
            Commands.RegisterCommand<AddValueToBucketCommand>();
        }

        public KeyValueBucket()
        {
            Values = new ConcurrentDictionary<string, EntityId>();
        }

        public override CommandController GetCommandController()
        {
            return Commands;
        }
    }
}