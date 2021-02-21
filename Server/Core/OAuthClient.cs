using System.Collections.Generic;
using System.Runtime.Serialization;
using Coflnet;

namespace Coflnet.Extentions.Oauth2 {

    /// <summary>
    /// Represents an oauth client application or some script capeable of performing commands.
    /// </summary>
    [DataContract]
    public class OAuthClient : Entity {
        private CommandController commandController = new CommandController ();

        private static Dictionary<string, OAuthClient> clients = new Dictionary<string, OAuthClient> ();

        public static OAuthClient Find (string id) {
            if (!clients.ContainsKey (id)) {
                throw new ClientNotFoundException ($"The client {id} wasn't found on this server");
            }
            return clients[id];
        }

        public override CommandController GetCommandController () {
            return commandController;
        }
/*
        [DataMember]
        private int id;
        /// <summary>
        /// The client owner
        /// </summary>

        [DataMember]
        private EntityId userId;
        [DataMember]
        private string name;
        [DataMember]
        private string publicId;
        [DataMember]
        private string description;
        [DataMember]
        private string secret;
        [DataMember]
        private string redirect;
        [DataMember]
        private bool revoked;
        [DataMember]
        private bool passwordClient;
        [DataMember]
        private string iconUrl;
        */
        [DataMember]
        public OAuthClientSettings settings;
        /// <summary>
        /// Custom commands registered at runtime
        /// </summary>
        [IgnoreDataMember]
        public CommandController customCommands;

        public class ClientNotFoundException : CoflnetException {
            public ClientNotFoundException (string message, string userMessage = null, string info = null, long msgId = -1) : base ("client_not_found", message, userMessage, 404, info, msgId) { }
        }
    }

}