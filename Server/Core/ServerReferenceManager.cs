using System;
using Coflnet;
using MessagePack;

namespace Coflnet.Server
{
	public class ServerReferenceManager : Coflnet.EntityManager
    {
		public override void ExecuteForReference(CommandData data, EntityId sender = default(EntityId))
		{
			if (data.Recipient.ServerId == 0) {
				// special case it is myself (0 is local)
				coreInstance.ExecuteCommand (data);
				return;
			}


			var mainManagingNode = ManagingNodeFor(data.Recipient);
			var IAmTheManager = mainManagingNode == CurrentServerId;

			InnerReference<Entity> reference;
			TryGetReference (data.Recipient, out reference);

			if (reference == null) {
				// is the current server the managing server?
				if (!IAmTheManager) {
					// we are not the main managing server, pass it on to it
					CoflnetCore.Instance.SendCommand (data);
					return;
				}
				// we are the main managing server but this resource doesn't exist
				throw new ObjectNotFound (data.Recipient);
			}

			//var amIAManagingNode = IsManagingNodeFor(this.coreInstance.Id,data.rId);
			var isTheSenderTheManager = IsManagingNodeFor(sender,data.Recipient);


			var resource = reference.Resource;

			if (resource != null) {
				var command = resource.GetCommandController ().GetCommand (data.Type);
				if(IAmTheManager)
				{
					// I can do everything
					resource.ExecuteCommand(data,command);
					
					if(command.Settings.Distribute)
					{
						// update
						UpdateSubscribers(resource,data);
					}


					// Not the only managing node? 
					// delay execution until they confirm update
					// coreInstance.SendCommand
					if(reference is RedundantInnerReference<Entity>)
					{
						var redRef = reference  as RedundantInnerReference<Entity>;
						SibblingUpdate(redRef,data,resource.GetHashCode(),()=>{
							coreInstance.SendCommand<ReceiveConfirm,ReceiveConfirmParams>(
							data.SenderId,new ReceiveConfirmParams(data.SenderId,data.MessageId),0,data.Recipient);
						});
					}

					// confirm execution except it is a confirm itself
					if(ReceiveConfirm.CommandSlug != command.Slug)
					{
						coreInstance.SendCommand<ReceiveConfirm,ReceiveConfirmParams>(
							data.SenderId,new ReceiveConfirmParams(data.SenderId,data.MessageId),0,data.Recipient);
					}
					// done
					return;
				}

				if (!command.Settings.Distribute) {
					// if the command isn't updating something, it is save to execute
					resource.ExecuteCommand (data, command);

					// THOUGHT: confirming may not be necessary since nonchanging commands return values otherwhise
					coreInstance.SendCommand<ReceiveConfirm,ReceiveConfirmParams>(
						data.SenderId,new ReceiveConfirmParams(data.SenderId,data.MessageId),0,data.Recipient);
				
					// the response should be returned now
					return;
				}

				// LocalPropagation? execute it right away
				if (command.Settings.LocalPropagation){
					// this is a command on its way to the manager but also needs to be applied now
					resource.ExecuteCommand (data, command);
				}
			}


			if(!IAmTheManager && (!isTheSenderTheManager)){
				// This message hasn't been on the manager yet, send it to him
				// this occurs if I am the senders manager
				coreInstance.SendCommand(data);
			}
		}


		/// <summary>
		/// Updates the sibbling nodes of the resource with the change
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="data"></param>
		/// <param name="hash"></param>
		/// <param name="callback"></param>
		public void SibblingUpdate(RedundantInnerReference<Entity> reference,CommandData data,int hash, Action callback)
		{
			int confirmCount = 0;

			foreach(var item in reference.SiblingNodes)
			{
				ServerCore.Instance.SendCommand<SibblingUpdate,CommandData>(new EntityId(item,0),data,ServerCore.Instance.Id,d=>
				{
					if(d.GetAs<int>() != hash)
					{
						// this server screwed up
						// forceclone TODO
						return;
					}


					confirmCount++;

					// yay, execution success
					if(confirmCount >= reference.SiblingNodes.Count)
					{
						callback?.Invoke();
					}
				});
			}
		}
	}



	public class ForceCloneCommand : Command
	{
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
		public override void Execute(CommandData data)
		{
			var resource = MessagePackSerializer.Typeless.Deserialize(data.message) as Entity;

			// make sure it exists, we are sibbling node and the main managing node is sending
			if(!data.CoreInstance.EntityManager.Exists(resource.Id) ||
				!data.CoreInstance.EntityManager.IsManagingNodeFor(data.CoreInstance.Id,resource.Id)
				|| data.CoreInstance.EntityManager.ManagingNodeFor(resource.Id) != data.SenderId )
			{
				throw new PermissionNotMetException("is_managingnode",resource.Id,data.SenderId,Slug);
			}

			// okay this is a valid clone
			data.CoreInstance.EntityManager.ReplaceResource(resource);
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
		public override string Slug => "forceClone";
	}
	

	
}