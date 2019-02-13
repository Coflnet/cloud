using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Coflnet
{
	/// <summary>
	/// Representation of a Coflnet user
	/// </summary>
	[DataContract]
	[System.Serializable]
	public class CoflnetUser : ReceiveableResource
	{
		private static CommandController commandController;


		/// <summary>
		/// Device add mode.
		/// 
		/// </summary>
		public enum DeviceAddMode
		{
			/// <summary>
			/// Does nothing to authenticate 
			/// </summary>
			NONE = 0,
			/// <summary>
			/// Just send the existing devices a notification
			/// </summary>
			NOTIFICATION,
			/// <summary>
			/// Requires one of email, sms or existing device
			/// </summary>
			AUTHENTICATION,
			/// <summary>
			/// Requires email and (sms or existing device)
			/// </summary>
			MULTIFACTOR_AUTHENTICATION,
			/// <summary>
			/// Requiring at least 2 existing devices to add the new one
			/// </summary>
			MULTIDEVICE
		}
		/// <summary>
		/// The name of the user.
		/// </summary>
		[IgnoreDataMember]
		public string userName;
		/// <summary>
		/// The first name of the user.
		/// </summary>
		[IgnoreDataMember]
		protected string firstName;
		/// <summary>
		/// The last name of the user.
		/// </summary>
		[IgnoreDataMember]
		protected string lastName;
		/// <summary>
		/// The email of the user.
		/// </summary>
		[IgnoreDataMember]
		protected string email;
		/// <summary>
		/// The users locale
		/// </summary>
		[IgnoreDataMember]
		protected string locale;
		/// <summary>
		/// The users group identifier.
		/// </summary>
		[IgnoreDataMember]
		protected string groupId;
		/// <summary>
		/// Optional extra values for this user.
		/// </summary>
		[IgnoreDataMember]
		protected Dictionary<string, string> keyValues;
		/// <summary>
		/// The users public ident key.
		/// </summary>
		[IgnoreDataMember]
		protected byte[] publicIdentKey;
		/// <summary>
		/// The users public pre key, signed by the ident key
		/// </summary>
		[IgnoreDataMember]
		protected byte[] signedPublicPreKey;

		/// <summary>
		/// A bunch of signed one time keys, signed by the Prekey.
		/// Used for establishing secure encrypted connection via
		/// the signal protocol.
		/// </summary>
		protected List<byte[]> signedPublicOneTimeKeys;
		/// <summary>
		/// The users age.
		/// </summary>
		[IgnoreDataMember]
		protected Int16 age;

		/// <summary>
		/// The users public identifier.
		/// </summary>
		[IgnoreDataMember]
		protected SourceReference publicId;

		/// <summary>
		/// How many Messages this user is allowed to send 
		/// </summary>
		[DataMember(Name = "cl")]
		private int callsLeft;
		/// <summary>
		/// The users friends
		/// </summary>
		[DataMember(Name = "f")]
		private List<Reference<CoflnetUser>> friends;
		/// <summary>
		/// UserIds of users which this user has silenced
		/// </summary>
		[DataMember(Name = "s")]
		protected List<Reference<CoflnetUser>> silent;
		/// <summary>
		/// UserIds of users which this one has blocked
		/// </summary>
		[DataMember(Name = "b")]
		protected List<Reference<CoflnetUser>> blocked;
		/// <summary>
		/// The users permissions
		/// </summary>
		[DataMember(Name = "p")]
		protected List<Permission> permissions;

		/// <summary>
		/// The users privacy settings
		/// true means allowed
		/// </summary>
		[DataMember(Name = "ps")]
		public Dictionary<string, bool> PrivacySettings;
		/// <summary>
		/// The profile image for this user
		/// </summary>
		[DataMember(Name = "pi")]
		public SourceReference ProfileImage;

		/// <summary>
		/// Devices allowed to access this users data
		/// </summary>
		[DataMember(Name = "d")]
		protected List<Device> devices;

		/// <summary>
		/// The device add mode controlls actions to be taken before and after a new device has been added
		/// </summary>
		[DataMember(Name = "dam")]
		protected DeviceAddMode deviceAddMode;

		/// <summary>
		/// Access tokens for third party APIs
		/// The index is the slug of the third party
		/// </summary>
		[DataMember(Name = "tpt")]
		protected Dictionary<string, ThirdPartyToken> thirdPartyTokens;

		/// <summary>
		/// Friend requests indexed by the userId of the other user
		/// </summary>
		[DataMember(Name = "fr")]
		public Dictionary<string, FriendRequest> FriendRequests;

		/// <summary>
		/// Files this user has uploaded
		/// </summary>
		[DataMember]
		protected Dictionary<long, UserFile> files;

		[DataMember]
		public byte[] Secret { get; set; }

		/// <summary>
		/// The auth token with wich the user authorized
		/// </summary>
		[IgnoreDataMember]
		protected string authToken;


		/// <summary>
		/// Called before the Call is processed, removes usage
		/// </summary>
		/// <param name="cost">Cost.</param>
		public void MadeAPICall(int cost = 1)
		{
			if (cost < 1)
			{
				throw new Exception("cost for an API call can't be less than 1");
			}
			callsLeft -= cost;
		}

		/// <summary>
		/// Adds a device.
		/// </summary>
		/// <param name="device">Device.</param>
		public void AddDevice(Device device)
		{
			devices.Add(device);
		}

		/// <summary>
		/// Whereether or not this users are friends
		/// </summary>
		/// <returns><c>true</c>, if friendship was found, <c>false</c> otherwise.</returns>
		/// <param name="userReference">User reference.</param>
		public bool IsFriend(Reference<CoflnetUser> userReference)
		{
			return friends.Contains(userReference);
		}

		/// <summary>
		/// Is this user set silent.
		/// </summary>
		/// <returns><c>true</c>, if user was silenced, <c>false</c> otherwise.</returns>
		/// <param name="userReference">User reference.</param>
		public bool IsSilenced(Reference<CoflnetUser> userReference)
		{
			return silent.Contains(userReference);
		}

		/// <summary>
		/// Is this user blocked.
		/// </summary>
		/// <returns><c>true</c>, if user was blocked, <c>false</c> otherwise.</returns>
		/// <param name="userReference">User reference.</param>
		public bool IsBlocked(Reference<CoflnetUser> userReference)
		{
			return blocked.Contains(userReference);
		}


		public void AcceptFriendRequest(FriendRequest request)
		{
			if (!request.TargetUser.Resource.publicId.Equals(this.publicId))
			{
				throw new CoflnetException("wrong_user", "The user for which accepted this friend request is not the one this request is for", "You can't accept this request");
			}
			// add him to our friend list
			friends.Add(request.RequestingUser);

			// tell his server to add him as well
			request.RequestingUser.ExecuteForResource(new MessageData(this.publicId, 0, "accepted_request"));
		}


		public void AcceptedFriendRequest(Reference<CoflnetUser> user)
		{
			// find the request on our side
			FriendRequest request = FriendRequests[user.Resource.publicId.ToString()];

			request.status = FriendRequest.RequestStatus.accepted;

			// add him to the friend list
			friends.Add(user);
		}


		public CoflnetUser() : base()
		{
			this.blocked = new List<Reference<CoflnetUser>>();
			this.silent = new List<Reference<CoflnetUser>>();
			this.friends = new List<Reference<CoflnetUser>>();
		}

		static CoflnetUser()
		{
			// register commands
			commandController = new CommandController();
			commandController.RegisterCommand<GetUserKeyValue>();
			commandController.RegisterCommand<SetUserKeyValue>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CoflnetUser"/> class.
		/// </summary>
		/// <param name="client">OauthClient creating this user.</param>
		public CoflnetUser(Application creater) : base(creater.Id)
		{
			this.publicId = ReferenceManager.Instance.CreateReference(this);
			this.blocked = new List<Reference<CoflnetUser>>();
			this.silent = new List<Reference<CoflnetUser>>();
			this.friends = new List<Reference<CoflnetUser>>();
		}

		public CoflnetUser(SourceReference owner) : base(owner)
		{
		}

		#region getter

		[IgnoreDataMember]
		public string UserName
		{
			get
			{
				return userName;
			}
			protected set
			{
				userName = value;
			}
		}
		[DataMember]
		public string FirstName
		{
			get
			{
				return firstName;
			}
			set
			{
				firstName = value;
			}
		}
		[DataMember]
		public string LastName
		{
			get
			{
				return lastName;
			}
			protected set
			{
				lastName = value;
			}
		}

		[DataMember]
		public byte[] PublicKey
		{
			get
			{
				return publicIdentKey;
			}
			set
			{
				publicIdentKey = value;
			}
		}

		[DataMember(Name = "sppk")]
		public byte[] SignedPublicPreKey
		{
			get
			{
				return signedPublicPreKey;
			}
			set
			{
				signedPublicPreKey = value;
			}
		}
		[DataMember(Name = "spotk")]
		public List<byte[]> SignedPublicOneTimeKeys
		{
			get
			{
				return signedPublicOneTimeKeys;
			}
			set
			{
				signedPublicOneTimeKeys = value;
			}
		}


		[DataMember]
		public short Age
		{
			get
			{
				return age;
			}
			set
			{
				age = value;
			}
		}

		[DataMember]
		public SourceReference PublicId
		{
			get
			{
				return publicId;
			}
			protected set
			{
				publicId = value;
			}
		}
		[DataMember]
		public int CallsLeft
		{
			get
			{
				return callsLeft;
			}
			set
			{
				callsLeft = value;
			}
		}

		[IgnoreDataMember]
		protected List<Device> Devices
		{
			get
			{
				return devices;
			}
			set
			{
				devices = value;
			}
		}
		[IgnoreDataMember]
		public List<Reference<CoflnetUser>> Friends
		{
			get
			{
				return friends;
			}

		}
		[IgnoreDataMember]
		public List<Reference<CoflnetUser>> Silent
		{
			get
			{
				return silent;
			}
			private set
			{
				silent = value;
			}
		}
		[IgnoreDataMember]
		public List<Reference<CoflnetUser>> Blocked
		{
			get
			{
				return blocked;
			}
			protected set
			{
				blocked = value;
			}
		}
		[IgnoreDataMember]
		public Dictionary<long, UserFile> Files
		{
			get
			{
				return files;
			}
		}
		[IgnoreDataMember]
		public string Locale
		{
			get; set;
		}

		[DataMember(Name = "kv")]
		public Dictionary<string, string> KeyValues
		{
			get
			{
				if (keyValues == null)
				{
					keyValues = new Dictionary<string, string>();
				}
				return keyValues;
			}
			set
			{
				keyValues = value;
			}
		}


		[IgnoreDataMember]
		public string AuthToken
		{
			get
			{
				return authToken;
			}
			set
			{
				authToken = value;
			}
		}

		[DataMember]
		public Dictionary<string, ThirdPartyToken> ThirdPartyTokens
		{
			get
			{
				return thirdPartyTokens;
			}
			set
			{
				thirdPartyTokens = value;
			}
		}


		#endregion

		public override CommandController GetCommandController()
		{
			return commandController;
		}

		public static CoflnetUser Generate(SourceReference owner)
		{
			var user = new CoflnetUser(owner);
			// generate a secret
			user.Secret = unity.libsodium.StreamEncryption.GetRandomBytes(16);
			return user;
		}

		public class GetPublicKeys : Command
		{
			public override void Execute(MessageData data)
			{
				// this is the current user
				var user = ReferenceManager.Instance.GetResource<CoflnetUser>(data.rId);
				var args = new EncryptionController.ReceivePublicKeys.Arguments()
				{
					id = user.Id,
					publicIdentKey = user.PublicKey,
					publicPreKey = user.SignedPublicPreKey
				};

				// if available add a onetimekey and destroy it afterwards
				if (user.SignedPublicOneTimeKeys.Count > 0)
				{
					args.oneTimeKey = user.SignedPublicOneTimeKeys[0];
					user.SignedPublicOneTimeKeys.RemoveAt(0);
				}

				ServerController.Instance.
								SendCommand<EncryptionController.ReceivePublicKeys,
				EncryptionController.ReceivePublicKeys.Arguments>(
									data.sId,
									args);
			}

			public override CommandSettings GetSettings()
			{
				return new CommandSettings();
			}

			public override string GetSlug()
			{
				return "getPublicKeys";
			}
		}
	}


	/// <summary>
	/// Gets a custom user key value.
	/// </summary>
	public class GetUserKeyValue : ServerCommand
	{
		public override void Execute(MessageData data)
		{
			String result;
			ReferenceManager.Instance
							.GetResource<CoflnetUser>(data.rId).KeyValues
							.TryGetValue(data.GetAs<string>(), out result);

			SendBack(data, data.Serialize(result));
		}

		public override ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings(true, 1, new IsUserPermission());
		}

		public override string GetSlug()
		{
			return "getValue";
		}
	}

	/// <summary>
	/// Sets an user key value.
	/// </summary>
	public class SetUserKeyValue : ServerCommand
	{
		public override void Execute(MessageData data)
		{
			UnityEngine.Debug.Log("data ist:  " + data.Data);
			var okay = data.GetAs<KeyValuePair<string, string>>();
			ReferenceManager.Instance
							.GetResource<CoflnetUser>(data.rId)
							.KeyValues[okay.Key] = okay.Value;
		}

		public override ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings(IsUserPermission.Instance, IsOwnerPermission.Instance);
		}

		public override string GetSlug()
		{
			return "setValue";
		}
	}


	/// <summary>
	/// Sets a value on an user object.
	/// </summary>
	public abstract class ValueSetter : ServerCommand
	{

		public override ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings(IsOwnerPermission.Instance);
		}
	}

	/// <summary>
	/// Gets a value from an user object.
	/// </summary>
	public abstract class ValueGetter : ServerCommand
	{

		public override ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings(true, 1, IsOwnerPermission.Instance);
		}
	}

	public class GetUserName : ValueGetter
	{
		public override void Execute(MessageData data)
		{
			SendBack(data, data.Serialize<string>(data.User.UserName));
		}

		public override string GetSlug()
		{
			return "getUserName";
		}
	}

	/// <summary>
	/// Get basic info about the user in one object
	/// </summary>
	public class GetBasicInfo : ValueGetter
	{
		public override void Execute(MessageData data)
		{
			var result = new InfoResult();
			var user = data.User;

			result.userName = user.userName;
			result.id = user.Id;
			if (user.PrivacySettings["share-profile-picture"])
			{
				result.profilePicture = user.ProfileImage;
			}


			SendBack(data, data.Serialize<InfoResult>(result));
		}

		public override string GetSlug()
		{
			return "getBasicInfo";
		}

		public class InfoResult
		{
			public SourceReference id;
			public SourceReference profilePicture;
			public string userName;
		}
	}


}

