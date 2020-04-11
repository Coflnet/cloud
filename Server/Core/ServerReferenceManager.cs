using System;
using Coflnet;
using MessagePack;

namespace Coflnet.Server
{
	public class ServerReferenceManager : Coflnet.ReferenceManager
    {
		public override void ExecuteForReference(MessageData data, SourceReference sender = default(SourceReference))
		{
			if (data.rId.ServerId == 0) {
				// special case it is myself (0 is local)
				coreInstance.ExecuteCommand (data);
				return;
			}


			var mainManagingNode = ManagingNodeFor(data.rId);
			var IAmTheManager = mainManagingNode == CurrentServerId;

			InnerReference<Referenceable> reference;
			TryGetReference (data.rId, out reference);

			if (reference == null) {
				// is the current server the managing server?
				if (!IAmTheManager) {
					// we are not the main managing server, pass it on to it
					CoflnetCore.Instance.SendCommand (data);
					return;
				}
				// we are the main managing server but this resource doesn't exist
				throw new ObjectNotFound (data.rId);
			}

			//var amIAManagingNode = IsManagingNodeFor(this.coreInstance.Id,data.rId);
			var isTheSenderTheManager = IsManagingNodeFor(sender,data.rId);


			var resource = reference.Resource;

			if (resource != null) {
				var command = resource.GetCommandController ().GetCommand (data.type);
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
					if(reference is RedundantInnerReference<Referenceable>)
					{
						var redRef = reference  as RedundantInnerReference<Referenceable>;
						SibblingUpdate(redRef,data,resource.GetHashCode(),()=>{
							coreInstance.SendCommand<ReceiveConfirm,ReceiveConfirmParams>(
							data.sId,new ReceiveConfirmParams(data.sId,data.mId),0,data.rId);
						});
					}

					// confirm execution except it is a confirm itself
					if(ReceiveConfirm.CommandSlug != command.Slug)
					{
						coreInstance.SendCommand<ReceiveConfirm,ReceiveConfirmParams>(
							data.sId,new ReceiveConfirmParams(data.sId,data.mId),0,data.rId);
					}
					// done
					return;
				}

				if (!command.Settings.Distribute) {
					// if the command isn't updating something, it is save to execute
					resource.ExecuteCommand (data, command);

					// THOUGHT: confirming may not be necessary since nonchanging commands return values otherwhise
					coreInstance.SendCommand<ReceiveConfirm,ReceiveConfirmParams>(
						data.sId,new ReceiveConfirmParams(data.sId,data.mId),0,data.rId);
				
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
		public void SibblingUpdate(RedundantInnerReference<Referenceable> reference,MessageData data,int hash, Action callback)
		{
			int confirmCount = 0;

			foreach(var item in reference.SiblingNodes)
			{
				ServerCore.Instance.SendCommand<SibblingUpdate,MessageData>(new SourceReference(item,0),data,ServerCore.Instance.Id,d=>
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
		/// <param name="data"><see cref="MessageData"/> passed over the network .</param>
		public override void Execute(MessageData data)
		{
			var resource = MessagePackSerializer.Typeless.Deserialize(data.message) as Referenceable;

			// make sure it exists, we are sibbling node and the main managing node is sending
			if(!data.CoreInstance.ReferenceManager.Exists(resource.Id) ||
				!data.CoreInstance.ReferenceManager.IsManagingNodeFor(data.CoreInstance.Id,resource.Id)
				|| data.CoreInstance.ReferenceManager.ManagingNodeFor(resource.Id) != data.sId )
			{
				throw new PermissionNotMetException("is_managingnode",resource.Id,data.sId,Slug);
			}

			// okay this is a valid clone
			data.CoreInstance.ReferenceManager.ReplaceResource(resource);
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