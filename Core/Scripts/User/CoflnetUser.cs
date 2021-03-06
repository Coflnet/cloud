﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Coflnet.Core;
using Coflnet.Core.User;
using MessagePack;

namespace Coflnet
{
    /// <summary>
    /// Representation of a Coflnet user
    /// </summary>
    
	[System.Serializable]
	[DataContract]
	public class CoflnetUser : ReceiveableResource {
		private static CommandController commandController;

		/// <summary>
		/// Device add mode.
		/// 
		/// </summary>
		public enum DeviceAddMode {
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

		[Key( "ofm")]
		public bool OnlyFriendsMessage;

		/// <summary>
		/// The name of the user.
		/// </summary>
		[Key("un")]
		public RemoteString UserName;


		/// <summary>
		/// References to App specific data. Eg settings or game Progress
		/// </summary>
		[Key("ad")]
		public RemoteDictionary<EntityId,Reference<ApplicationData>> appData;

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
		/// The users birthDay.
		/// </summary>
		[IgnoreDataMember]
		protected DateTime birthDay;

		/// <summary>
		/// How many Messages this user is allowed to send 
		/// </summary>
		[Key( "cl")]
		private int callsLeft;
		/// <summary>
		/// The users friends
		/// </summary>
		[Key( "f")]
		private List<Reference<CoflnetUser>> friends;
		/// <summary>
		/// UserIds of users which this user has silenced
		/// </summary>
		[Key( "s")]
		protected List<Reference<CoflnetUser>> silent;
		/// <summary>
		/// UserIds of users which this one has blocked
		/// </summary>
		[Key( "b")]
		protected List<Reference<CoflnetUser>> blocked;

		/// <summary>
		/// The users privacy settings
		/// true means allowed
		/// </summary>
		[Key( "ps")]
		public Dictionary<string, bool> PrivacySettings;
		/// <summary>
		/// The profile image for this user
		/// </summary>
		[Key( "pi")]
		public EntityId ProfileImage;

		/// <summary>
		/// Devices allowed to access this users data
		/// </summary>
		[Key( "d")]
		protected List<Device> devices;

		/// <summary>
		/// The device add mode controlls actions to be taken before and after a new device has been added
		/// </summary>
		[Key( "dam")]
		protected DeviceAddMode deviceAddMode;

		/// <summary>
		/// Access tokens for third party APIs
		/// The index is the slug of the third party
		/// </summary>
		[Key( "tpt")]
		protected Dictionary<string, ThirdPartyToken> thirdPartyTokens;

		/// <summary>
		/// Friend requests indexed by the userId of the other user
		/// </summary>
		[Key( "fr")]
		public Dictionary<string, FriendRequest> FriendRequests;

		/// <summary>
		/// Files this user has uploaded
		/// </summary>
		[DataMember]
		protected Dictionary<long, UserFile> files;

		[Key("secret")]
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
		public void MadeAPICall (int cost = 1) {
			if (cost < 1) {
				throw new Exception ("cost for an API call can't be less than 1");
			}
			callsLeft -= cost;
		}

		/// <summary>
		/// Adds a device.
		/// </summary>
		/// <param name="device">Device.</param>
		public void AddDevice (Device device) {
			devices.Add (device);
		}

		/// <summary>
		/// Whereether or not this users are friends
		/// </summary>
		/// <returns><c>true</c>, if friendship was found, <c>false</c> otherwise.</returns>
		/// <param name="userReference">User reference.</param>
		public bool IsFriend (Reference<CoflnetUser> userReference) {
			return friends.Contains (userReference);
		}

		public byte[] Serialize () {
			return MessagePack.MessagePackSerializer.Serialize<CoflnetUser> (this);
		}

		/// <summary>
		/// Is this user set silent.
		/// </summary>
		/// <returns><c>true</c>, if user was silenced, <c>false</c> otherwise.</returns>
		/// <param name="userReference">User reference.</param>
		public bool IsSilenced (Reference<CoflnetUser> userReference) {
			return silent.Contains (userReference);
		}

		/// <summary>
		/// Is this user blocked.
		/// </summary>
		/// <returns><c>true</c>, if user was blocked, <c>false</c> otherwise.</returns>
		/// <param name="userReference">User reference.</param>
		public bool IsBlocked (Reference<CoflnetUser> userReference) {
			return blocked.Contains (userReference);
		}

		public void AcceptFriendRequest (FriendRequest request) {
			if (!request.TargetUser.Resource.Id.Equals (this.Id)) {
				throw new CoflnetException ("wrong_user", "The user for which accepted this friend request is not the one this request is for", "You can't accept this request");
			}
			// add him to our friend list
			friends.Add (request.RequestingUser);

			// tell his server to add him as well
			request.RequestingUser.ExecuteForEntity (new CommandData (this.Id, 0, "accepted_request"));
		}

		public void AcceptedFriendRequest (Reference<CoflnetUser> user) {
			// find the request on our side
			FriendRequest request = FriendRequests[user.Resource.Id.ToString ()];

			request.status = FriendRequest.RequestStatus.accepted;

			// add him to the friend list
			friends.Add (user);
		}

		public CoflnetUser () : base () {
			this.blocked = new List<Reference<CoflnetUser>> ();
			this.silent = new List<Reference<CoflnetUser>> ();
			this.friends = new List<Reference<CoflnetUser>> ();

		}

		static CoflnetUser () {
			// register commands
			commandController = new CommandController (persistenceCommands);
			commandController.RegisterCommand<GetUserKeyValue> ();
			commandController.RegisterCommand<SetUserKeyValue> ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CoflnetUser"/> class.
		/// </summary>
		/// <param name="creater">OauthClient creating this user.</param>
		public CoflnetUser (Application creater) : base (creater.Id) {
			this.Id = EntityManager.Instance.CreateReference (this);
			this.blocked = new List<Reference<CoflnetUser>> ();
			this.silent = new List<Reference<CoflnetUser>> ();
			this.friends = new List<Reference<CoflnetUser>> ();
		}

		public CoflnetUser (EntityId owner) : base (owner) {
			this.blocked = new List<Reference<CoflnetUser>> ();
			this.silent = new List<Reference<CoflnetUser>> ();
			this.friends = new List<Reference<CoflnetUser>> ();
		}

		#region getter



		[Key("fn")]
		public string FirstName {
			get {
				return firstName;
			}
			set {
				firstName = value;
			}
		}

		[Key("ln")]
		public string LastName {
			get {
				return lastName;
			}
			set {
				lastName = value;
			}
		}


		[Key( "sppk")]
		public byte[] SignedPublicPreKey {
			get {
				return signedPublicPreKey;
			}
			set {
				signedPublicPreKey = value;
			}
		}

		[Key( "spotk")]
		public List<byte[]> SignedPublicOneTimeKeys {
			get {
				return signedPublicOneTimeKeys;
			}
			set {
				signedPublicOneTimeKeys = value;
			}
		}

		[Key( "bd")]
		public DateTime Birthday {
			get {
				return birthDay;
			}
			set {
				birthDay = value;
			}
		}


		[Key("cl")]
		public int CallsLeft {
			get {
				return callsLeft;
			}
			set {
				callsLeft = value;
			}
		}

		[IgnoreDataMember]
		protected List<Device> Devices {
			get {
				return devices;
			}
			set {
				devices = value;
			}
		}

		[IgnoreDataMember]
		public List<Reference<CoflnetUser>> Friends {
			get {
				return friends;
			}
		}

		[IgnoreDataMember]
		public List<Reference<CoflnetUser>> Silent {
			get {
				return silent;
			}
			private set {
				silent = value;
			}
		}

		[IgnoreDataMember]
		public List<Reference<CoflnetUser>> Blocked {
			get {
				return blocked;
			}
			protected set {
				blocked = value;
			}
		}

		[IgnoreDataMember]
		public Dictionary<long, UserFile> Files {
			get {
				return files;
			}
		}

		[IgnoreDataMember]
		public string Locale {
			get;
			set;
		}

		[Key( "kv")]
		public Dictionary<string, string> KeyValues {
			get {
				if (keyValues == null) {
					keyValues = new Dictionary<string, string> ();
				}
				return keyValues;
			}
			set {
				keyValues = value;
			}
		}

		[IgnoreDataMember]
		public string AuthToken {
			get {
				return authToken;
			}
			set {
				authToken = value;
			}
		}

		[Key( "tpt")]
		public Dictionary<string, ThirdPartyToken> ThirdPartyTokens {
			get {
				return thirdPartyTokens;
			}
			set {
				thirdPartyTokens = value;
			}
		}

		#endregion

		public override CommandController GetCommandController () {
			return commandController;
		}

		public static CoflnetUser Generate (EntityId owner, EntityManager referenceManager = null) {
			var user = new CoflnetUser (owner);
			// generate a secret
			user.Secret = unity.libsodium.StreamEncryption.GetRandomBytes (16);
			user.AssignId (referenceManager);
			return user;
		}

		public class GetPublicKeys : Command {
			public override void Execute (CommandData data) {
				// this is the current user
				var user = EntityManager.Instance.GetEntity<CoflnetUser> (data.Recipient);
				var args = new EncryptionController.ReceivePublicKeys.Arguments () {
					id = user.Id,
						publicIdentKey = user.publicKey,
						publicPreKey = user.SignedPublicPreKey
				};

				// if available add a onetimekey and destroy it afterwards
				if (user.SignedPublicOneTimeKeys.Count > 0) {
					args.oneTimeKey = user.SignedPublicOneTimeKeys[0];
					user.SignedPublicOneTimeKeys.RemoveAt (0);
				}

				ServerController.Instance.
				SendCommand<EncryptionController.ReceivePublicKeys, EncryptionController.ReceivePublicKeys.Arguments> (
					data.SenderId,
					args);
			}

			protected override CommandSettings GetSettings () {
				return new CommandSettings ();
			}

			public override string Slug {
				get {

					return "getPublicKeys";
				}
			}
		}
	}

	/// <summary>
	/// Gets a custom user key value.
	/// </summary>
	public class GetUserKeyValue : ReturnCommand {
		public override CommandData ExecuteWithReturn (CommandData data) {
			String result;
			data.GetTargetAs<CoflnetUser> ()
				.KeyValues
				.TryGetValue (data.GetAs<string> (), out result);

			var returnData = new CommandData ();
			returnData.SerializeAndSet<String> (result);
			return returnData;
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug {
			get {

				return "getValue";
			}

		}
	}

	/// <summary>
	/// Sets an user key value.
	/// </summary>
	public class SetUserKeyValue : ValueSetter {
		public override void Execute (CommandData data) {
			var okay = data.GetAs<KeyValuePair<string, string>> ();


			data.CoreInstance.EntityManager
				.GetEntity<CoflnetUser> (data.Recipient)
				.KeyValues[okay.Key] = okay.Value;
		}

		public override string Slug {
			get {

				return "setValue";
			}
		}

	}

	/// <summary>
	/// Sets a value on an user object.
	/// </summary>
	public abstract class ValueSetter : Command {

		protected override CommandSettings GetSettings () {
			return new CommandSettings (false, true, false, true, WritePermission.Instance);
		}
	}

	/// <summary>
	/// Gets a value from an user object.
	/// </summary>
	public abstract class ValueGetter : ServerCommand {

		public override ServerCommandSettings GetServerSettings () {
			return new ServerCommandSettings (ReadPermission.Instance);
		}
	}

	public class GetUserName : ValueGetter {
		public override void Execute (CommandData data) {
			SendBack (data, data.Serialize<string> (data.GetTargetAs<CoflnetUser> ().UserName));
		}

		public override string Slug {
			get {

				return "getUserName";
			}
		}
	}


    /// <summary>
    /// Get basic info about the user in one object
    /// </summary>
    public class GetBasicInfo : Command {
		public override void Execute (CommandData data) {
			var result = new PublicUserInfo ();
			var user = EntityManager.Instance.GetEntity<CoflnetUser> (data.Recipient);

			result.userName = user.UserName;
			result.GetAccess().Owner = user.Id;
			
			if (user.PrivacySettings["share-profile-picture"]) {
				result.profilePicture = user.ProfileImage;
			}

			data.SendBack (CommandData.CreateCommandData<BasicInfoResponse, PublicUserInfo> (data.SenderId, result));
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings (IsAuthenticatedPermission.Instance, IsNotBockedPermission.Instance);
		}

		public override string Slug {
			get {

				return "getBasicInfo";
			}

		}
	}

	public class BasicInfoResponse : Command {
		public override void Execute (CommandData data) {
			throw new NotImplementedException ();
		}

		protected override CommandSettings GetSettings () {
			throw new NotImplementedException ();
		}

		public override string Slug {
			get {

				throw new NotImplementedException ();
			}
		}

	}
}