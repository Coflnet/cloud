using Coflnet;
using Coflnet.Core;
using System.Linq;

namespace Coflnet.Client.Messaging
{
    public class IsChatMember : Coflnet.Permission
    {
        /// <summary>
        /// An instance of this <see cref="Permission"/> class since usually only one is required.
        /// </summary>
        public static IsChatMember Instance;
        static IsChatMember () {
            Instance = new IsChatMember ();
        }

        /// <summary>
        /// Execute the command logic with specified data.
        /// </summary>
        /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
        /// <param name="target">The local <see cref="Referenceable"/> on which to test on .</param>
        public override bool CheckPermission (MessageData data, Referenceable target) {
            var chat = target as GroupChatResource;
            if(chat == null)
            {
                return false;
            }
            return chat.Members.Any(i => i.userId == data.sId);
        }

        public override string Slug => "IsChatMember";
    }
}