namespace Coflnet.Core.Commands
{
    /// <summary>
    /// Subscribes to a resource
    /// </summary>
    public class SubscribeCommand : Command
    {
        public override string Slug => "sub";

        public override void Execute(CommandData data)
        {
            data.GetTargetAs<Entity>().GetAccess().Subscribe(data.SenderId);
        }

        protected override CommandSettings GetSettings()
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
        /// <param name="data"><see cref="CommandData"/> passed over the network .</param>
        public override void Execute(CommandData data)
        {
            // who do we want to subscribe to
            data.CoreInstance.CloneAndSubscribe(data.GetAs<EntityId>(),r => {
                // send it back further

                // add the client to subscriber list
                r.GetAccess().Subscribe(data.SenderId);
            });
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
        public override string Slug => "sub2";
    }
    

}