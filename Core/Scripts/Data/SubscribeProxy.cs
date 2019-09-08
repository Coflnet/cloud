using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet.Core
{
    /// <summary>
    /// Temporary proxy till cloning of resource is done
    /// </summary>
    [DataContract]
    public class SubscribeProxy : Referenceable,IProxyReferenceable
    {
        

        [DataMember]
        public List<MessageData> buffer = new List<MessageData>();

        public SubscribeProxy(SourceReference id)
        {
            this.Id = id;
        }

        public override CommandController GetCommandController()
        {
            return new CommandController();
        }

        public override Command ExecuteCommand(MessageData data)
        {
            buffer.Add(data);

            return new ProxyCommand();
        }

        public class ProxyCommand : Command
        {
            public override string Slug => "proxy";

            public override void Execute(MessageData data)
            {
                data.GetTargetAs<Referenceable>().ExecuteCommand(data);
            }

            protected override CommandSettings GetSettings()
            {
                return new CommandSettings(false,false,false);
            }
        }
        
	}
}