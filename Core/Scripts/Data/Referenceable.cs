using System.Collections.Generic;
using System.Runtime.Serialization;
using Coflnet.Core.Commands;
using MessagePack;

namespace Coflnet
{
    /// <summary>
    /// Defines objects that are <see cref="Entity"/> across servers.
    /// Only use for larger objects.
    /// </summary>
    [DataContract]
	public abstract class Entity {
		[DataMember]
		[Key("Id")]
		public virtual EntityId Id {get;set;}

		[DataMember]
		[Key("a")]
		public Access Access;

		/// <summary>
		/// Global commands useable for every <see cref="Entity"/> in the system.
		/// Contains commands for syncing resources between servers.
		/// </summary>
		[IgnoreDataMember]
		public static CommandController globalCommands;

		protected Entity (Access access, EntityId id) {
			this.Id = id;
			this.Access = access;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Coflnet.Server.<see cref="Entity"/>"/> class.
		/// </summary>
		/// <param name="owner">Owner creating this resource.</param>
		protected Entity (EntityId owner) : this () {
			this.Access = new Access (owner);
		}

		public Entity () {

			//this.Id = ReferenceManager.Instance.CreateReference(this);
		}

		/// <summary>
		/// Initializes the <see cref="T:Coflnet.<see cref="Entity"/>"/> class.
		/// </summary>
		static Entity () {
			globalCommands = new CommandController ();
			globalCommands.RegisterCommand<ReturnResponseCommand> ();
			globalCommands.RegisterCommand<GetResourceCommand>();
			globalCommands.RegisterCommand<CreationResponseCommand>();
			globalCommands.RegisterCommand<SubscribeCommand>();
		}

		/// <summary>
		/// Assigns an identifier and registers the object in the <see cref="EntityManager"/>
		/// </summary>
		/// <param name="referenceManager">Optional other instance of an referencemanager</param>
		public void AssignId (EntityManager referenceManager = null) {
			if (referenceManager != null) {
				this.Id = referenceManager.CreateReference (this);
			} else {
				this.Id = EntityManager.Instance.CreateReference (this);
			}
		}

		/// <summary>
		/// Checks if a certain resource is allowed to access this one and or execute commands
		/// </summary>
		/// <returns><c>true</c>, if allowed access, <c>false</c> otherwise.</returns>
		/// <param name="requestingReference">Requesting reference.</param>
		/// <param name="mode">Mode.</param>
		public virtual bool IsAllowedAccess (EntityId requestingReference, AccessMode mode = AccessMode.READ) {
            return (Access != null) && Access.IsAllowedToAccess (requestingReference, mode,this.Id)
				// A resource might access itself
				||
				requestingReference == this.Id;
		}

		/// <summary>
		/// Executes the command found in the <see cref="CommandData.Type"/>
		/// Returns the <see cref="Command"/> when done
		/// </summary>
		/// <returns>The command.</returns>
		/// <param name="data">Data.</param>
		public virtual Command ExecuteCommand (CommandData data) {
			var controller = GetCommandController ();
			var command = controller.GetCommand (data.Type);

			controller.ExecuteCommand (command, data, this);

			return command;
		}

		/// <summary>
		/// Executes the command with given data
		/// </summary>
		/// <param name="data">Data to pass on to the <see cref="Command"/>.</param>
		/// <param name="command">Command to execute.</param>
		public virtual void ExecuteCommand (CommandData data, Command command) {
			GetCommandController ().ExecuteCommand (command, data, this);
		}

		public abstract CommandController GetCommandController ();

		/// <summary>
		/// Will generate and add a new Access Instance if none exists yet.
		/// </summary>
		/// <returns>The Access Settings for this Entity</returns>
		public Access GetAccess()
		{
			if(Access == null){
				Access = new Access();
			}
			return Access;
		}

        public override bool Equals(object obj)
        {
            var entity = obj as Entity;
            return entity != null &&
                   EqualityComparer<EntityId>.Default.Equals(Id, entity.Id);
        }

        public override int GetHashCode()
        {
            var hashCode = -681095413;
            hashCode = hashCode * -1521134295 + EqualityComparer<EntityId>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<Access>.Default.GetHashCode(Access);
            return hashCode;
        }
    }
}