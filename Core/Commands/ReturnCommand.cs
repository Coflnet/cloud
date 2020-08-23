using System.Collections;
using System.Collections.Generic;
using Coflnet;
using System;
using MessagePack;

namespace Coflnet
{
	/// <summary>
	/// Base class for Commands just returning a value that may be lost.
	/// </summary>
	public abstract class ReturnCommand : Command
	{
		public override void Execute(CommandData data)
		{
			var returnData = ExecuteWithReturn(data);
			// set headers
			returnData.Type = "response";
			returnData.Recipient = data.SenderId;
			returnData.SenderId = data.Recipient;

			// wrap it into a container so receiver knows to what request it coresponds to
			returnData.message = IEncryption.ConcatBytes(BitConverter.GetBytes(data.MessageId), returnData.message);
			data.SendBack(returnData);
			//SendTo(data.sId, user.PublicId, "createdUser");
		}


		public abstract CommandData ExecuteWithReturn(CommandData data);

		protected override CommandSettings GetSettings()
		{
			return new CommandSettings(true,true,false,false);
		}

	}

	/// <summary>
	/// Special command for tunneling a response based flow (like http)
	/// <see cref="ReturnCommand"/> isn't persisted nor distributed.
	/// </summary>
	public class ReturnResponseCommand : Command
	{
		/// <summary>
		/// Execute the command logic with specified data.
		/// </summary>
		/// <param name="data"><see cref="CommandData"/> passed over the network .</param>
		public override void Execute(CommandData data)
		{
			// The command may not be present anymore
			long id = BitConverter.ToInt64(data.message, 0);
			byte[] dataWithoutId = new byte[data.message.Length - 8];
			Array.Copy(data.message,8,dataWithoutId,0,dataWithoutId.Length);
			//data.message.CopyTo(dataWithoutId, 8);
			data.message = dataWithoutId;


			ReturnCommandService.Instance.ReceiveMessage(id, data);
		}

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		protected override CommandSettings GetSettings()
		{
			return new CommandSettings();
		}
		/// <summary>
		/// Gets the globally unique slug (short human readable id).
		/// </summary>
		/// <returns>The slug .</returns>
		public override string Slug => "response";
	}


	/// <summary>
	/// Special base class for commands creating Resources
	/// </summary>
	public abstract class CreationCommand : Command
	{
		/// <summary>
		/// Creates the resource and returns its identifier
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public abstract Entity CreateResource(CommandData data);

		public override void Execute(CommandData data)
		{
			EntityId oldId;
			try 
			{
				oldId = data.GetAs<CreationParamsBase>().options.OldId;
			}catch(Exception)
			{
				throw new CoflnetException("invalid_payload","The payload of the command isn't of type CreationParamsBase nor derived from it");
			}
			var resource = CreateResource(data);

			resource.AssignId(data.CoreInstance.EntityManager);

			// make sure the owner is set
			resource.GetAccess().Owner = data.Recipient;

			// add the size to any data cap limit (tracking module)
			// TODO 
			

			// it is possible that the return will not be received by the target in case it gets offline

			data.SendBack(CommandData.CreateCommandData<CreationResponseCommand,KeyValuePair<EntityId,EntityId>>
				(data.SenderId,new KeyValuePair<EntityId,EntityId>(oldId,resource.Id)));
		}

		
        protected override CommandSettings GetSettings()
        {
            return new CommandSettings(true,false,false,WritePermission.Instance);
        }


		/// <summary>
		/// Base class for creation params
		/// </summary>
		[MessagePackObject]
		public class CreationParamsBase
		{
			/// <summary>
			/// Options for the creation of the object
			/// </summary>
			[Key(0)]
			public Options options;

			/// <summary>
			/// Additional Options
			/// </summary>
			[MessagePackObject]
			public class Options
			{
				/// <summary>
				/// The temporarly client side assigned id 
				/// </summary>
				[Key(0)]
				public EntityId OldId;

				/// <summary>
				/// The prefered (nearest) Region of the client to host 
				/// </summary>
				[Key(1)]
				public EntityId preferedRegion;
			}

			public CreationParamsBase()
			{
				options = new Options();
			}
		}
	}

    public abstract class ReceivableCreationCommand : CreationCommand
    {

        public override Entity CreateResource(CommandData data)
        {
            var res = CreateReceivable(data);
			res.publicKey = data.GetAs<Params>().KeyPair.publicKey;
			return res;
        }

		protected abstract ReceiveableResource CreateReceivable(CommandData data);


		[MessagePackObject]
		public class Params : CreationParamsBase
		{
			[Key(1)]
			public SigningKeyPair KeyPair;
		}
    }



    public class CreationResponseCommand : Command
    {
        public override string Slug => "creationResponse";

        public override void Execute(CommandData data)
        {
			var pair = data.GetAs<KeyValuePair<EntityId,EntityId>>();
            
			data.CoreInstance.EntityManager.UpdateIdAndAddRedirect(pair.Key,pair.Value);
        }

        protected override CommandSettings GetSettings()
        {
            return new CommandSettings();
        }
    }
}

