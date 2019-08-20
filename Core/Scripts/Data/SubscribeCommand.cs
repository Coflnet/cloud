namespace Coflnet.Core.Commands
{
    /// <summary>
    /// Subscribes to a resource
    /// </summary>
    public class SubscribeCommand : Command
    {
        public override string Slug => "sub";

        public override void Execute(MessageData data)
        {
            data.GetTargetAs<Referenceable>().GetAccess().Subscribe(data.sId);
        }

        public override CommandSettings GetSettings()
        {
            return new CommandSettings(ReadPermission.Instance);
        }
    }

    /// <summary>
    /// Instructs a server to subscribe to something
    /// </summary>
    public class Sub2Command : Command
    {
        /// <summary>
        /// Execute the command logic with specified data.
        /// </summary>
        /// <param name="data"><see cref="MessageData"/> passed over the network .</param>
        public override void Execute(MessageData data)
        {
            // who do we want to subscribe to
            data.CoreInstance.CloneAndSubscribe(data.GetAs<SourceReference>(),r => {
                // send it back further

                // add the client to subscriber list
                r.GetAccess().Subscribe(data.sId);
            });
        }

        /// <summary>
        /// Special settings and Permissions for this <see cref="Command"/>
        /// </summary>
        /// <returns>The settings.</returns>
        public override CommandSettings GetSettings()
        {
            return new CommandSettings( );
        }
        /// <summary>
        /// The globally unique slug (short human readable id) for this command.
        /// </summary>
        /// <returns>The slug .</returns>
        public override string Slug => "sub2";
    }
    

}