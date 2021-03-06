﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Coflnet;
using Coflnet.Core.Crypto;
using MessagePack;

public class EncryptionController {
	/*
	 *  User options:
	 * - End to end encryption (multiple devices) key is only exchanged directly
	 * - Key is stored encrypted in the cloud
	 * - Key is stored in plain (secured by account)
	 * 
	 * [] backup chats
	 */

	public static EncryptionController Instance;

	private Dictionary<EntityId, Encrypt> encryptionInstances;
	private Dictionary<EntityId, EndToEndEncrypt> endToEndEncryptInstances;
	private Dictionary<ValueTuple<EntityId, EntityId>, EndToEndEncrypt> groupEndToEndEncryptInstances;
	private UserKeys ownKeys;
	private UserKeys oldOwnKeys;
	private LinkedList<EntityId> userIdDownloadChain;
	private Dictionary<EntityId, long> userTimeouts;

	public delegate void DecryptedMessageCallback (EntityId from, string message);

	private string keyPairString;

	void Awake () {

	}


	public EncryptionController () {

		encryptionInstances = new Dictionary<EntityId, Encrypt> ();
		userTimeouts = new Dictionary<EntityId, long> ();
		userIdDownloadChain = new LinkedList<EntityId> ();
	}

	static EncryptionController () {
		Instance = new EncryptionController ();

		var cc = CoflnetCore.CoreCommands;
		cc.RegisterCommand (new LegacyCommand ("sessionSetup", Instance.ReceiveSessionSetup));
		cc.RegisterCommand (new LegacyCommand ("resendSetup", Instance.RequestResendSetup));
		cc.RegisterCommand<ReceivedSetup> ();

	}

	/// <summary>
	/// Gets the end to end encrypt.
	/// Will generate a new EndToEndEncrypt instance if none is present
	/// </summary>
	/// <returns>The end to end encrypt.</returns>
	/// <param name="partner">Partner.</param>
	public EndToEndEncrypt GetEndToEndEncrypt (EntityId partner) {
		if (endToEndEncryptInstances.ContainsKey (partner))
			return endToEndEncryptInstances[partner];
		if (!DataController.encryptionLoaded) {
			throw new Exception ("Master file encryption key has to loaded/entered/derived bevore chain keys can be loaded");
		}
		// try to load it
		try {
			return DataController.Instance.LoadObject<EndToEndEncrypt> (partner.ToString ());
		} catch (Exception) {
			return new EndToEndEncrypt (partner);
		}
	}

	/// <summary>
	/// Gets the end to end encrypt.
	/// </summary>
	/// <returns>The end to end encrypt.</returns>
	/// <param name="partner">Partner.</param>
	/// <param name="group">Group .</param>
	public EndToEndEncrypt GetEndToEndEncrypt (EntityId partner, EntityId group) {
		var key = new ValueTuple<EntityId, EntityId> (partner, group);
		if (groupEndToEndEncryptInstances.ContainsKey (key))
			return groupEndToEndEncryptInstances[key];
		if (!DataController.encryptionLoaded) {
			throw new Exception ("Master file encryption key has to loaded/entered/derived bevore chain keys can be loaded");
		}
		// try to load it
		try {
			groupEndToEndEncryptInstances[key] = DataController.Instance.LoadObject<GroupEndToEndEncryption> (key.ToString ());
			return groupEndToEndEncryptInstances[key];
		} catch (Exception) {
			return new EndToEndEncrypt (partner);
		}
	}


	/// <summary>
	/// The Active secure enough to use, hopefully unbrocken Signgin Algrorytm to use by the application
	/// </summary>
	/// <returns></returns>
	public SigningAlgorythm SigningAlgorythm => new LibsodiumSignature();

	/// <summary>
	/// Saves the end to end encrypt.
	/// </summary>
	/// <param name="encryptInstance">Encrypt instance.</param>
	private void SaveEndToEndEncrypt (EndToEndEncrypt encryptInstance) {
		DataController.Instance.SaveObject (encryptInstance.Identifier.ToString (), encryptInstance);
	}

	/// <summary>
	/// Saves the group end to end encrypt.
	/// </summary>
	/// <param name="encryptInstance">Encrypt instance.</param>
	private void SaveEndToEndEncrypt (GroupEndToEndEncryption encryptInstance) {
		var key = new ValueTuple<EntityId, EntityId> (encryptInstance.Identifier, encryptInstance.GroupId);
		DataController.Instance.SaveObject (encryptInstance.Identifier.ToString (), encryptInstance);
	}

	void SaveAllEncInstances () {
		foreach (var item in encryptionInstances) {
			SaveEncInstance (item.Key, item.Value);
		}
	}

	public void FirstMessage (string id) {

	}

	/// <summary>
	/// Receives a message and trys to decrypt it.
	/// </summary>
	/// <param name="from">From.</param>
	/// <param name="message">Message.</param>
	/// <param name="callback">Callback to pass the message to.</param>
	public void ReceiveMessage (EntityId from, string message, DecryptedMessageCallback callback = null) {
		string decryptedMessage = DecryptFriendMessage (from, message);
		if (callback != null && decryptedMessage != null)
			callback (from, decryptedMessage);
	}

	/// <summary>
	/// Encrypts the .msg of a ServerMessageOb for some reference.
	/// </summary>
	/// <returns>The newly encrypted message for friend.</returns>
	/// <param name="to">To.</param>
	/// <param name="message">ServerMessageOb.</param>
	public CommandData EncryptMessageForFriend (EntityId to, CommandData message) {
		var encryptedMessageOb = new CommandData (message);
		Encrypt partner = GetEncrypt (to);
		if (partner.GetIdentKey () == null || !partner.HasSessionKeys ()) {
			to.ExecuteForEntity<CoflnetUser.GetPublicKeys, string> ("");

			throw new Exception ("Partner keys aren't loaded yet");
		}

		byte[] encryptedMessage = partner.EncryptWithSessionKey (message.message);

		//	if (!partner.HasReceivedSetup())
		//		encryptedMessageOb.h = partner.GetSessionSetupHeaders();

		encryptedMessageOb.message = encryptedMessage;

		return encryptedMessageOb;
	}

	/// <summary>
	/// Decrypts a message from friend.
	/// </summary>
	/// <returns>The decrypted message from friend.</returns>
	/// <param name="from">From.</param>
	/// <param name="message">Message.</param>
	public CommandData DecryptMessageFromFriend (EntityId from, CommandData message) {
		Encrypt partner = GetEncrypt (from);

		// if the setup hasn't been receive, friend keys aren't present or 
		// partner is sending us header and has correct identifier(he instantiated a own session)
		/*	if (partner.GetIdentKey() == null || !partner.HasSessionKeys() ||
				message.h != null)// && !partner.GetIsServer() || message.header != null && partner.GetIdentKey() == message.header.publicIdentKey)
			{
				ReceiveSessionSetup(from, message.h);
			}
			else if (!partner.HasReceivedSetup())
			{
				partner.DestroyTempKeys();
				partner.ReceivedSetup();
				SaveEncInstance(from, partner);
			} */

		message.message = partner.DecryptWithSessionKey (message.message);

		return message;
	}

	/// <summary>
	/// Receives the session setup.
	/// </summary>
	/// <param name="data">Data.</param>
	public void ReceiveSessionSetup (CommandData data) {
		ReceiveSessionSetup (data.SenderId, data.GetAs<ChatSetupHeader> ());
	}

	public void ReceiveSessionSetup (EntityId from, ChatSetupHeader header) {
		var partner = GetEncrypt (from);
		var p2 = new LibsodiumEncryption ();

		if (header == null)
			throw new NullReferenceException ("Error:/ message doesn't contain setup headers and session keys are missing");
		if (header.publicOneTimeKey != null) {
			KeyPair oneTimeKeyPair = FindOneTimeKey (header.publicOneTimeKey);
			partner.SetServerKeys (header, oneTimeKeyPair);
		} else
			partner.SetServerKeys (header, null);
		if (partner.DeriveKeysServer ()) {
			// save that we received the session, prevents resending requests
			partner.ReceivedSetup ();
			// deriving was successful, send confirmation
			SendCommand<ReceivedSetup, EntityId> (from, from);
		} else {
			throw new Exception ("key deriviation failed");
		}
	}

	public class SessionSetup : Command {
		public override void Execute (CommandData data) {
			var encrypt = Instance.GetEndToEndEncrypt (data.SenderId);
			var setup = data.GetAs<ChatSetupHeader> ();
			var ephermeralKeyPair = KeyPairManager.Instance.GetKeyPair (setup.publicOneTimeKey);

			bool result = encrypt.DeriveKeysServer (setup.publicPreKey, setup.publicEphemeralKey, ephermeralKeyPair);

			if (result) {
				// deriving was successful, send confirmation
				ServerController.Instance.SendCommand<ReceivedSetup, EntityId> (data.SenderId, data.SenderId);
			} else {
				throw new Exception ("key deriviation failed");
			}
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings (false, false);
		}

		public override string Slug {
			get {

				return "sessionSetup";
			}
		}
	}

	public class ResendSetup : Command {
		public override void Execute (CommandData data) {
			Instance.SendSetup (data.SenderId);
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings (false, false);
		}

		public override string Slug {
			get {

				return "resendSetup";
			}
		}
	}

	public EntityId Me {
		get {
			return ConfigController.UserSettings.userId;
		}
	}

	public class ReceivedSetup : Command {
		public override void Execute (CommandData data) {
			if (new EntityId (data.Data) != Instance.Me) {
				Track.instance.SendTrackingRequest ("session setup failed userId info is: " + data.Data);

				// this is unexpected
				throw new Exception ("session setup failed");
				// possible fixes are:
				// creating a new session
				// resubmitting session Setup
				// To avoid destroying current session none of them will happen
			}

			Instance.GetEndToEndEncrypt (data.SenderId).DestroyTempKeys ();
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings (false, true);
		}

		public override string Slug {
			get {

				return "receivedSetup";
			}
		}
	}

	/// <summary>
	/// Decrypts a message sent from a group resource.
	/// </summary>
	/// <returns>The decrypted group message.</returns>
	/// <param name="message">encrypted message sent from the server.</param>
	public CommandData DecryptGroupMessage (CommandData message) {
		var decryptedMessage = new CommandData (message);
		var messageContent = message.GetAs<EncryptedChatMessage> ();
		var chatEncrypt = GetEndToEndEncrypt (messageContent.Sender, message.SenderId);
		decryptedMessage.message = chatEncrypt.DecryptWithSessionKey (messageContent.EncryptedMessage);
		decryptedMessage.SenderId = messageContent.Sender;
		return decryptedMessage;
	}

	[MessagePack.MessagePackObject]
	public class EncryptedChatMessage {
		[MessagePack.Key (1)]
		public EntityId Sender;
		[MessagePack.Key (2)]
		public byte[] EncryptedMessage;
	}

	/// <summary>
	/// Encrypts a message for a group resource.
	/// Requires that the receiverId of the <see cref="CommandData"/> has been set to the receiverid of the group
	/// </summary>
	/// <returns>The group message.</returns>
	/// <param name="messageOb">Message to encrypt.</param>
	public CommandData EncryptGroupMessage (CommandData messageOb) {
		var myId = ConfigController.UserSettings.userId;
		var encrypt = GetEndToEndEncrypt (myId, messageOb.Recipient);

		var encryptedMessage = new EncryptedChatMessage ();
		encryptedMessage.EncryptedMessage = encrypt.EncryptWithSessionKey (messageOb.message);
		encryptedMessage.Sender = myId;
		// create and return encrypted object
		var encryptedCommandData = new CommandData (messageOb);
		encryptedCommandData.SerializeAndSet (encryptedMessage);

		return encryptedCommandData;
	}

	/// <summary>
	/// Sends a command.
	/// </summary>
	/// <param name="to">To.</param>
	/// <param name="data">Data.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	/// <typeparam name="Y">The 2nd type parameter.</typeparam>
	public void SendCommand<T, Y> (EntityId to, Y data) where T : Command {
		ServerController.Instance.SendCommand<T, Y> (to, data);
	}

	/// <summary>
	/// Encrypts something for a resource.
	/// </summary>
	/// <returns>The for resource.</returns>
	/// <param name="data">The data to encrypt</param>
	public byte[] EncryptForResource (EntityId to, byte[] data) {
		var partner = GetEndToEndEncrypt (to);
		return partner.EncryptWithSessionKey (data);
	}

	/// <summary>
	/// Decrypts an encrypted command.
	/// </summary>
	/// <returns>The decrypted command data.</returns>
	/// <param name="data">Encrypted command data Object as json.</param>
	public void ReceiveEncryptedCommand (Command command, CommandData data) {
		Encrypt partner = GetEncrypt (data.SenderId);
		if (!partner.HasSessionKeys ()) {
			data.SenderId.ExecuteForEntity<ResendSetup, char> (' ');
			throw new EncryptionKeyIsMissingException ("session keys are currently missing");
		}

		SecureCommandData secureCommandData = new SecureCommandData (data.Data);
		data.message = partner.DecryptWithSessionKey (secureCommandData.encMsg, secureCommandData.msgIndex);
		command.Execute (data);
	}

	/// <summary>
	/// Ratchets a given key x times
	/// </summary>
	/// <returns>The ratcheted key</returns>
	/// <param name="key">Key.</param>
	/// <param name="x">Max.</param>
	private byte[] RatchetKey (byte[] key, int x) {
		byte[] tmpKey = key;
		for (int i = 0; i < x; i++) {
			tmpKey = Encrypt.RatchetKey (key);
		}
		return tmpKey;
	}

	private KeyPair FindOneTimeKey (byte[] publicPart) {
		for (int i = 0; i < ownKeys.oneTimeKeys.Length; i++) {
			if (ownKeys.oneTimeKeys[i].publicKey.SequenceEqual (publicPart))
				return ownKeys.oneTimeKeys[i];
		}
		if (oldOwnKeys != null) {
			for (int i = 0; i < oldOwnKeys.oneTimeKeys.Length; i++) {
				if (oldOwnKeys.oneTimeKeys[i].publicKey.SequenceEqual (publicPart))
					return oldOwnKeys.oneTimeKeys[i];
			}
		}
		//		throw new UnityException("Error:/ OneTimeKey not found ");
		return null;
	}

	public string DecryptFriendMessage (EntityId from, string message) {
		Encrypt enc = GetEncrypt (from);
		try {

			return enc.DecryptAES (message);
		} catch (EncryptionKeyIsMissingException) {
			// this may be the first message and therefor there isn't a key yet?
			try {
				string encryptionKey = enc.DecryptBase64 (message);
				if (encryptionKey.Length == 16)
					SetKeyForInstall (from, encryptionKey);
			} catch (Exception ex) {
				// This is bad, realy bad :( 
				// display an message to the user
				Track.instance.SendTrackingRequest (ex.ToString ());
			}
			return null;
		}
	}

	/// <summary>
	/// Generates crypto secure string.
	/// </summary>
	/// <returns>The crypto string.</returns>
	/// <param name="length">Length default 16 (256bits).</param>
	public string GenerateCryptoString (int length = 16) {
		byte[] randomByte = new byte[length];
		Encrypt.FillRandomBytes (randomByte);
		byte[] salt = GetSalt ();

		randomByte = exclusiveOR (salt, randomByte);

		return Encoding.ASCII.GetString (randomByte);
	}

	private string calcualteNewSalt (string input) {
		return Encoding.ASCII.GetString (Encrypt.SHA256 (input));
	}

	private byte[] GetSalt () {
		var r = new System.Random (DateTime.UtcNow.GetHashCode ());
		var b = new byte[16];
		r.NextBytes (b);
		return b;
	}

	public static byte[] BitArrayToByteArray (BitArray bits) {
		byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
		bits.CopyTo (ret, 0);
		return ret;
	}

	public static byte[] exclusiveOR (byte[] arr1, byte[] arr2) {
		if (arr1.Length != arr2.Length)
			throw new ArgumentException ("arr1 and arr2 are not the same length");

		byte[] result = new byte[arr1.Length];

		for (int i = 0; i < arr1.Length; ++i)
			result[i] = (byte) (arr1[i] ^ arr2[i]);

		return result;
	}

	public void GenerateKeys () {
		if (ownKeys != null)
			oldOwnKeys = ownKeys;
		ownKeys = new UserKeys ();
		ownKeys.preKey = Encrypt.NewKeypair ();
		ownKeys.identKey = Encrypt.NewSingKeypair ();
		for (int i = 0; i < 64; i++) {
			ownKeys.oneTimeKeys[i] = Encrypt.NewKeypair ();
		}

		SaveOwnKeys (true);
		Encrypt.SetOwnKeys (ownKeys, null);
	}

	public void SetEncInstance (UserKeys enc) {
		ownKeys = enc;
	}

	public void SaveOwnKeys (bool upload = false) {
		DataController.Instance.SaveObject ("ownKeys", ownKeys);
		DataController.Instance.SaveObject ("oldOwnKeys", oldOwnKeys);
	}

	private string SignKey (byte[] publicKey, KeyPair identKey, byte[] expireTimestamp) {
		byte[] empKeyWithTimestamp = Encrypt.ConcatBytes (publicKey, expireTimestamp);
		//Logger.Log ("unsinged key: " +JsonUtility.ToJson( new KeyPair(expireTimestamp,empKeyWithTimestamp)) );
		byte[] signedOneTimeKey = Encrypt.SignByte (empKeyWithTimestamp, identKey);
		return Convert.ToBase64String (signedOneTimeKey);
	}

	public void LoadKeysAsync () {
		DataController.Instance.LoadObjectAsync<UserKeys> ("ownKeys", data => {
			Instance.ownKeys = data;
			EndToEndEncrypt.SetOwnKeys (data, null);
		});
		DataController.Instance.LoadObjectAsync<UserKeys> ("oldOwnKeys", data => {
			Instance.oldOwnKeys = data;
		});
	}

	public void RequestResendSetup (CommandData data) {
		SendSetup (data.SenderId);
	}

	public void SendSetup (EntityId sId) {
		// this should be secure as the keys are set to null after receive confirmation
		sId.ExecuteForEntity<ReceivedSetup, ChatSetupHeader> (
			GetEndToEndEncrypt (sId).GetSessionSetupHeaders ());
	}

	private string GetPublicKey (EntityId userId) {
		return ValuesController.GetString (userId + "publicKey");
	}

	private void SaveEncInstance (EntityId userId, Encrypt encryptInstance) {
		FileController.SaveAs<Encrypt> (userId + "encrypt", encryptInstance);
	}

	public void SetKeyForInstall (EntityId userId, string key) {
		ValuesController.SetString (userId + "encKey", key);
		GetEncrypt (userId).SetAESKey (key);
	}

	private Encrypt GetEncrypt (EntityId userId) {
		if (encryptionInstances.ContainsKey (userId))
			return encryptionInstances[userId];

		string saveName = userId + "encrypt";

		if (FileController.Exists (saveName)) {
			Encrypt loadedEnc = FileController.LoadAs<Encrypt> (saveName);
			encryptionInstances[userId] = loadedEnc;
			return loadedEnc;
		}

		Encrypt newEnc = new Encrypt (null, userId);
		encryptionInstances[userId] = newEnc;
		return newEnc;
	}

	public void DeleteEnc (EntityId userId) {
		if (encryptionInstances.ContainsKey (userId))
			encryptionInstances.Remove (userId);

		ValuesController.DeleteKey (userId + "encrypt");
	}

	/// <summary>
	/// Receives public keys.
	/// Instantiates a new encryption chanel if not present.
	/// </summary>
	public class ReceivePublicKeys : Command {
		public override void Execute (CommandData data) {
			var args = data.GetAs<Arguments> ();

			byte[] publicIdentKey = args.publicIdentKey;
			byte[] publicPreKey = EndToEndEncrypt.SignByteOpen (args.publicPreKey, publicIdentKey);

			var encrypt = Instance.GetEndToEndEncrypt (args.id);

			// validate identity
			if (encrypt.PublicIdentKey != null && !encrypt.PublicIdentKey.SequenceEqual (publicIdentKey)) {
				throw new Exception ("Incorrect ident key abording");
			}

			encrypt.DeriveKeysClient (publicIdentKey, publicPreKey, args.oneTimeKey);

			Instance.SaveEndToEndEncrypt (encrypt);

			// send the setup headers
			Instance.SendSetup (args.id);

		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug {
			get {

				return "receivePublicKeys";
			}
		}

		public class Arguments {
			public EntityId id;
			public byte[] publicIdentKey;
			public byte[] publicPreKey;
			public byte[] oneTimeKey;
		}
	}

}

[System.Serializable]
public class ChatKeys {
	public EntityId identifier;
	public byte[] receiveKey;
	/// <summary>
	/// The current message chat identifier, aka index of the last message.
	/// </summary>
	public int cmcid;
	public bool isAdmin;

	public ChatKeys (EntityId identifier) {
		this.identifier = identifier;
	}
	public ChatKeys () {

	}
}

/// <summary>
/// Represents an encrypted message with nonce/keychain index
/// </summary>
[System.Serializable]
[MessagePackObject]
public class SecureCommandData {
	[Key (0)]
	public ulong msgIndex;
	[Key (1)]
	public byte[] encMsg;

	/// <summary>
	/// Initializes a new instance of the <see cref="T:SecureCommandData"/> class from data received from <see cref="this.ToString"/>
	/// </summary>
	/// <param name="msgAsString">Message as string.</param>
	public SecureCommandData (string msgAsString) : this (Convert.FromBase64String (msgAsString)) {

	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:SecureCommandData"/> class.
	/// Reverse of GetBytes();
	/// </summary>
	/// <param name="msgBytes">Message bytes.</param>
	public SecureCommandData (byte[] msgBytes) {
		this.encMsg = new byte[msgBytes.Length - 8];
		Array.Copy (msgBytes, 8, this.encMsg, 0, encMsg.Length);
		this.msgIndex = BitConverter.ToUInt16 (msgBytes, 0);
	}

	public SecureCommandData (byte[] encMsg, ulong msgIndex) {
		this.encMsg = encMsg;
		this.msgIndex = msgIndex;
	}

	public SecureCommandData (CommandData data) : this (data.message) {

	}

	public override string ToString () {
		return Convert.ToBase64String (GetBytes ());
	}

	public byte[] GetBytes () {
		return IEncryption.ConcatBytes (BitConverter.GetBytes (msgIndex), encMsg);
	}
}