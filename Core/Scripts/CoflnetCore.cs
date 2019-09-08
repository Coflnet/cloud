using System;
using Coflnet.Core;
using Coflnet.Core.Commands;

namespace Coflnet
{
	/// <summary>
	/// Provides core functionallity for the coflnet ServerSystem
	/// Allows to reuse code server and client side.
	/// Instance is set to the correct server or client version of the core.
	/// </summary>
	public abstract class CoflnetCore : Referenceable
	{
		private static CoflnetCore _instance;
		private static CommandController _coreCommands= new CommandController(Referenceable.globalCommands);

		/// <summary>
		/// Commands all cores share. Exists because core instance might not exist yet.
		/// </summary>
		/// <returns></returns>
		public static CommandController CoreCommands 
		{
			get
			{
				return _coreCommands;
			}
		}


		/// <summary>
		/// Event triggered on ApplicationExit.
		/// You might need this if you need to save something bevore exiting.
		/// </summary>
		public event Action OnApplicationExit;

		/// <summary>
		/// Instance of an <see cref="ReferenceManager"> used for executing commands
		/// </summary>
		/// <value></value>
		public ReferenceManager ReferenceManager{get;set;}


		/// <summary>
		/// Invokes the <see cref="OnApplicationExit"/> event
		/// </summary>
		public void InvokeOnExit()
		{
			OnApplicationExit?.Invoke();
		}


		/// <summary>
		/// Is set to the correct server or client version of the core when calling the 
		/// `Init` method on the correct `Core` eg. `ClientCore`
		/// </summary>
		/// <value>a</value>
		public static CoflnetCore Instance
		{
			get
			{
				if (_instance == null)
				{
					throw new Exception("No instance has been set yet");
				}
				return _instance;
			}
			protected set
			{
				_instance = value;
			}
		}

		/// <summary>
		/// Receives and processes a command.
		/// Counterpart to <see cref="SendCommand"/>
		/// </summary>
		/// <param name="data">Command data received from eg. the network</param>

		/// <param name="sender">The vertified sender of the command, controls if the command is executed right away or only sent to the managing server</param>
		public void ReceiveCommand(MessageData data, SourceReference sender = default(SourceReference))
		{
			// validate the sender if possible
			ReceiveableResource resource;
			this.ReferenceManager.TryGetResource(data.sId,out resource);

			if(resource != null && resource.publicKey != null)
			{
				if(!data.ValidateSignature(resource.publicKey))
				{
					throw new CoflnetException("invalid_signature",$"The signature of the message `{data.sId}:{data.mId}` could not be vertified");
				}
			}
			

			this.ReferenceManager.ExecuteForReference(data,sender);

			//SendCommand<ReceiveConfirm,ReceiveConfirmParams>(data.sId,new ReceiveConfirmParams(data.sId,data.mId),0,data.rId);
		}


		public abstract void SendCommand(MessageData data, long serverId = 0);
		/// <summary>
		/// Sends a command.
		/// </summary>
		/// <param name="receipient">Receipient to send to.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="id">Unique Identifier of the message.</param>
		/// <param name="sender">Optional sender, if not sent the core will attempt to replace with active Referenceable eg CoflnetUser.</param>
		/// <typeparam name="C"><see cref="Command"/> to send.</typeparam>
		/// <typeparam name="T">Type of <paramref name="data"/> needed for seralization.</typeparam>
		public abstract void SendCommand<C, T>(SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference)) where C : Command;
		public abstract void SendCommand<C>(SourceReference receipient, byte[] data) where C : Command;


		/// <summary>
		/// Sends a command that doesn't have any data
		/// </summary>
		/// <param name="receipient">Receipient to send to</param>
		/// <param name="id">ID of the message (optional)</param>
		/// <param name="sender">As who to send the command, required for permission checking</param>
		/// <typeparam name="C">Command class to send</typeparam>
		public void SendCommand<C>(SourceReference receipient,long id = 0, SourceReference sender = default(SourceReference)) where C:Command
		{
			SendCommand<C,short>(receipient,0,id,sender);
		}


		/// <summary>
		/// Sends a command that returns a value, allows a callback to be passed.
		/// </summary>
		/// <param name="receipient">Receipient to send to.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="callback">Callback to be executed when the response is received.</param>
		/// <typeparam name="C"><see cref="Command"/> to send.</typeparam>
		/// <typeparam name="T">Type of <paramref name="data"/> needed for seralization.</typeparam>
		public void SendCommand<C, T>(SourceReference receipient, T data, Command.CommandMethod callback) where C : ReturnCommand
		{
			SendCommand<C,T>(receipient,data,default(SourceReference),callback);
		}


		/// <summary>
		/// Sends a command that returns a value, allows a callback to be passed.
		/// </summary>
		/// <param name="receipient">Receipient to send to.</param>
		/// <param name="data">Data to send.</param>
		/// <param name="sender">Identity under which to send the command.</param>
		/// <param name="callback">Callback to be executed when the response is received.</param>
		/// <typeparam name="C"><see cref="Command"/> to send.</typeparam>
		/// <typeparam name="T">Type of <paramref name="data"/> needed for seralization.</typeparam>
		public void SendCommand<C, T>(SourceReference receipient, T data, SourceReference sender, Command.CommandMethod callback) where C : ReturnCommand
		{
			long id = ThreadSaveIdGenerator.NextId;
			ReturnCommandService.Instance.AddCallback(id, callback);

			if(sender == default(SourceReference))
			{
				sender = this.Id;
			}

			SendCommand<C, T>(receipient, data, id,sender);
		}



		/// <summary>
		/// Sends a command that returns a value, allows a callback to be passed.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="callback"></param>
		/// <typeparam name="T"></typeparam>
		public void SendGetCommand(MessageData data, Command.CommandMethod callback) 
		{
			if(String.IsNullOrEmpty(data.type))
			{
				throw new ArgumentException("Command identifier has not been set");
			}

			long id = ThreadSaveIdGenerator.NextId;
			ReturnCommandService.Instance.AddCallback(id, callback);

			if(data.sId == default(SourceReference))
			{
				data.sId = this.Id;
			}

			SendCommand(data);
		}


		/// <summary>
		/// Executes a command, will  attempt to apply it localy bevore sending it to the managing node
		/// </summary>
		/// <param name="receipient"></param>
		/// <param name="data"></param>
		/// <param name="id"></param>
		/// <param name="sender"></param>
		/// <typeparam name="C"></typeparam>
		/// <typeparam name="T"></typeparam>

		public void ExecuteCommand<C,T>(SourceReference receipient, T data, long id = 0, SourceReference sender = default(SourceReference)) where C :Command
		{

			var commandInstance = ((C)Activator.CreateInstance(typeof(C)));

 			var messageData = MessageData.SerializeMessageData<T>(data, commandInstance.Slug, id);

			messageData.rId = receipient;
			messageData.sId = sender;


			ReferenceManager.Instance.ExecuteForReference(messageData);
		}


		/// <summary>
		/// Clones and subscribes to updates for a resource, returns the local resource if it were already cloned
		/// </summary>
		/// <param name="resourceId">Id of the resource to clone</param>
		/// <param name="afterClone">Callback invoked when cloning is done</param>
		public virtual void CloneAndSubscribe(SourceReference resourceId, Action<Referenceable> afterClone = null)
		{
			// if it already exists we are done here
			if(ReferenceManager.Exists(resourceId))
			{
				afterClone?.Invoke(ReferenceManager.GetResource<Referenceable>(resourceId));
				return;
			}

			// create temporary proxy to receive commands bevore cloning is finished
			ReferenceManager.AddReference(new SubscribeProxy(resourceId));

			UnityEngine.Debug.Log($"Subscribing to {resourceId} from {Id}");

			// this is different on client sides
			SendCommand<SubscribeCommand>(resourceId,0,Id);


			// now clone it
			FinishSubscribing(resourceId,afterClone);
		}

		protected void FinishSubscribing(SourceReference resourceId, Action<Referenceable> afterClone)
		{
			SendCommand<GetResourceCommand,short>(resourceId,0,o =>{
				var resource = MessagePack.MessagePackSerializer.Typeless.Deserialize(o.message) as Referenceable;

				// detach the old proxy
				var proxy = ReferenceManager.GetResource<SubscribeProxy>(resourceId);

				// replace it
				ReferenceManager.ReplaceResource(resource);

				var test = ReferenceManager.GetResource<Referenceable>(resourceId);

				UnityEngine.Debug.Log($"Cloned and replaced {resource.Id} on {Id}({this.GetType().Name}) type: {test.GetType().Name}");

				// replay messages
				foreach (var data in proxy.buffer)
				{
					resource.ExecuteCommand(data);
				}

				afterClone?.Invoke(resource);
			});
		}


	}
}
