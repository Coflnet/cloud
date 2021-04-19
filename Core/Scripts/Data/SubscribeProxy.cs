using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet.Core
{
    /// <summary>
    /// Temporary proxy till cloning of resource is done
    /// </summary>
    [DataContract]
    public class SubscribeProxy : Entity,IProxyEntity
    {
        

        [DataMember]
        public List<CommandData> buffer = new List<CommandData>();

        public SubscribeProxy(EntityId id)
        {
            this.Id = id;
        }

        public override CommandController GetCommandController()
        {
            return new CommandController();
        }

        public override Command ExecuteCommand(CommandData data,Command passedCommand = null)
        {
            buffer.Add(data);

            return new ProxyCommand();
        }

        public class ProxyCommand : Command
        {
            public override string Slug => "proxy";

            public override void Execute(CommandData data)
            {
                data.GetTargetAs<Entity>().ExecuteCommand(data);
            }

            protected override CommandSettings GetSettings()
            {
                return new CommandSettings(false,false,false);
            }
        }
        
	}
}