using Coflnet;

namespace Coflnet.Core
{
    /// <summary>
    /// The <see cref="MessageTranstitController"/> makes sure a message arrives and is executed at the receiver side.
    /// It takes care of the Delivery of a command including routing and encrypting.
    /// </summary>
    public class MessageTranstitController
    {

        /// <summary>
        /// An instance of the <see cref="MessageTranstitController"/> class since usually only one is required.
        /// </summary>
        public static MessageTranstitController Instance;
        static MessageTranstitController () {
            Instance = new MessageTranstitController ();
        }


        public void Deliver(CommandData data)
        {
            if (data == null)
            {
                throw new System.ArgumentNullException(nameof(data));
            }


            CommandDataPersistence.Instance.SaveMessage(data);


            Send(data);
        }

        protected void Send(CommandData data)
        {
            // differs from side to side
            var referenceManager = data.CoreInstance.EntityManager;

            var target = referenceManager.ManagingNodeFor(data.Recipient);
            CoflnetServer server;
            referenceManager.TryGetEntity(target,out server);

            if(server == null)
            {
                // unknown server
                // get info / send to managing node
                return;
            }


            // client needs to use the main managing node to send 
            // server (maybe client) need to send to non-instant-connectable devices (offline)

            server.GetOrCreateConnection().SendCommand(data);
        }

        protected void RetrySend()
        {
            foreach (var item in CommandDataPersistence.Instance.GetAllUnsent())
            {
                Send(item);
            }
        }
    }
}