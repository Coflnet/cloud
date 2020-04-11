using Coflnet;
using Coflnet.Client;

namespace Coflnet.Client.Messaging
{
    public class ChatMessageCommand : Command
    {
        /// <summary>
        /// Execute the command logic with specified data.
        /// </summary>
        /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
        public override void Execute(MessageData data)
        {
                          ChatService.Instance.ReceiveMessage(data.GetAs<ChatMessage>(),data.sId);
        }
        /// <summary>
        /// Special settings and Permissions for this <see cref="Command"/>
        /// </summary>
        /// <returns>The settings.</returns>
        protected override CommandSettings GetSettings()
        {
            return new CommandSettings( );
        }
        /// <summary>
        /// The globally unique slug (short human readable id) for this command.
        /// </summary>
        /// <returns>The slug .</returns>
        public override string Slug => "msg";
    }
}
