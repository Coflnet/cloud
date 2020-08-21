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
        /// <param name="data"><see cref="CommandData"/> passed over the network .</param>
        /// <param name="target">The local <see cref="Entity"/> on which to test on .</param>
        public override bool CheckPermission (CommandData data, Entity target) {
            var chat = target as GroupChatResource;
            if(chat == null)
            {
                return false;
            }
            return chat.Members.Any(i => i.userId == data.SenderId);
        }

        public override string Slug => "IsChatMember";
    }
}