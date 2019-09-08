﻿using MessagePack;
using Coflnet;
using Coflnet.Core;


namespace Coflnet
{
	public partial class ReferenceManager
	{
		/// <summary>
		/// Gets public info for some <see cref="Referenceable"/>, 
		/// Loads a proxy to the target <see cref="Referenceable"/> if it doesn't exist and isn't accessable publicly.
		/// Clones and subscribes to <see cref="PublicInfo"/> if not done already.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="onLoad"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetPublicInfo<T>(SourceReference owner,System.Action<T> onLoad) where T : PublicInfo
		{
			Referenceable ownerRes;
			if(!TryGetResource<Referenceable>(owner,out ownerRes))
			{
				// doesn't exist try to get it
				coreInstance.SendCommand<GetPublicInfoCommand,short>(owner,0,response=>{
					var placeHolder = response.GetAs<HasPublicInfoPlaceholder>();
					AddReference(response.GetAs<HasPublicInfoPlaceholder>());

					// clone and subscribe to the actual info
					coreInstance.CloneAndSubscribe(
						placeHolder.PublicInfo.ReferenceId,
						value=>{
							// invoke the callback
							onLoad.Invoke(value as T);
						});
				});


				throw new ObjectNotFound(owner);
			}
			var infoRef = ownerRes as IHasPublicInfo;
			if(ownerRes == null)
			{
				throw new System.Exception("The target is no IHasPublicInfo");
			}

			return infoRef.PublicInfo.Resource as T;
		}
	}
}




namespace Coflnet.Core
{
	public class GetPublicInfoCommand : ReturnCommand
	{
		public override MessageData ExecuteWithReturn(MessageData data)
        {
			var infoReference = data.GetTargetAs<Referenceable>() as IHasPublicInfo;
			// this is a seperate object to be serialized more easily
			var placeHolder = new HasPublicInfoPlaceholder(infoReference);
            data.SerializeAndSet(placeHolder);
			return data;
        }

		/// <summary>
		/// Special settings and Permissions for this <see cref="Command"/>
		/// </summary>
		/// <returns>The settings.</returns>
		protected override CommandSettings GetSettings()
		{
			return new CommandSettings(true,false,false,false);
		}

  

        /// <summary>
        /// The globally unique slug (short human readable id) for this command.
        /// </summary>
        /// <returns>The slug .</returns>
        public override string Slug => "GetPublicInfo";
	}
	



	/// <summary>
	/// This <see cref="Referenceable"/> has another <see cref="Referenceable"/> 
	/// that is publicly accessable information about this one. 
	/// Eg. an user implements this and it points to a public Profile.
	/// </summary>
	public interface IHasPublicInfo  {
		Reference<PublicInfo> PublicInfo {get;}
	
	}

	/// <summary>
	/// Base class for public information about another <see cref="Referenceable"/>
	/// </summary>
	public abstract class PublicInfo : Referenceable
	{
		
	}






	/// <summary>
	/// Local placeholder for object we don't have Access to but has public information we want
	/// </summary>
	[MessagePackObject]
    public class HasPublicInfoPlaceholder : Referenceable, IHasPublicInfo,IProxyReferenceable
    {
		[Key("pi")]
        public Reference<PublicInfo> PublicInfo
        {
            get;set;
        }

        public override CommandController GetCommandController()
        {
            throw new System.NotImplementedException();
        }


		public HasPublicInfoPlaceholder(IHasPublicInfo resource)
		{
			this.PublicInfo = resource.PublicInfo;
		}
    }

	/// <summary>
	/// Interface to tell the <see cref="ReferenceManager"/> that this isn't the real object.
	/// </summary>
	public interface IProxyReferenceable
	{

	}
}