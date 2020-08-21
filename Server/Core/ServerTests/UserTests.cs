using System.Collections;
using System.Linq;
using Coflnet;
using Coflnet.Server;
using NUnit.Framework;

public class CoflnetUserTests {

	#if UNITY_EDITOR

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator CreateUserTest () {
		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference (1, 1, 1, 0);
		ServerCore.Init ();
		ClientSocket.Instance.Reconnect ();
		CommandData response = null;
		ClientSocket.Instance.AddCallback (data => {
			response = data;
		});

		yield return new UnityEngine.WaitForSeconds (1);

		// set data
		var request = new RegisterUserRequest ();
		request.captchaToken = "";
		request.clientId = ConfigController.ApplicationSettings.id;

		// send the command
		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<RegisterUser, RegisterUserRequest> (ConfigController.ApplicationSettings.id, request));

		// await response
		yield return new UnityEngine.WaitForSeconds (0.5f);
		Logger.Log (response.GetAs<RegisterUserResponse> ().id);

		// user was created in the last second
		Assert.IsTrue (response.GetAs<RegisterUserResponse> ().id.ResourceId > ThreadSaveIdGenerator.NextId - 10000000);

		ServerCore.Stop ();
	}

	public class TestCommandWithPermission : Command {
		public override void Execute (CommandData data) {
			data.SendBack (CommandData.SerializeCommandData (5, "success"));
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings (false, false, false, IsAuthenticatedPermission.Instance);
		}

		public override string Slug => "testCommandPermission";

	}

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator CreateAndLoginUserTest () {
		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference (1, 1, 1, 0);
		ServerCore.Init ();
		CommandData response = null;
		ClientSocket.Instance.AddCallback (data => {
			response = data;
		});

		yield return new UnityEngine.WaitForSeconds (1);

		// set data
		var request = new RegisterUserRequest ();
		request.captchaToken = "";
		request.clientId = ConfigController.ApplicationSettings.id;

		// send the command
		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<RegisterUser, RegisterUserRequest> (ConfigController.ApplicationSettings.id, request));

		// await response
		yield return new UnityEngine.WaitForSeconds (0.5f);

				var login = response.GetAs<RegisterUserResponse> ();

		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<Coflnet.LoginUser, LoginParams> (ConfigController.ApplicationSettings.id,
				new LoginParams () {
					id = login.id,
						secret = login.secret
				}));

		// tell the client that we are logged in
		ConfigController.ActiveUserId = login.id;

		yield return new WaitForSeconds (0.5f);

		// register test command on user
		(new CoflnetUser ()).GetCommandController ().OverwriteCommand<TestCommandWithPermission> ();

		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<TestCommandWithPermission, int> (login.id, 1), true);

		yield return new WaitForSeconds (0.5f);

		// user was created in the last second
		Assert.IsTrue (response.GetAs<int> () == 5);
	}

	/// <summary>
	///Tries to send a message to an offline user and tests message persistence
	/// </summary>
	[UnityTest]
	public IEnumerator MessageTransitPersitenceTest () {
		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference (1, 1, 1, 0);
		ServerCore.Init ();
		CommandData response = null;
		ClientSocket.Instance.Reconnect ();
		ClientSocket.Instance.AddCallback (data => {
			response = data;
		});

		ConfigController.ActiveUserId = SourceReference.Default;

		yield return new UnityEngine.WaitForSeconds (0.5f);

		// set data
		var request = new RegisterUserRequest ();
		request.captchaToken = "";
		request.clientId = ConfigController.ApplicationSettings.id;

		// send the command
		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<RegisterUser, RegisterUserRequest> (ConfigController.ApplicationSettings.id, request));

		// await response
		yield return new UnityEngine.WaitForSeconds (0.5f);

		var login = response.GetAs<RegisterUserResponse> ();

		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<Coflnet.LoginUser, LoginParams> (
				ConfigController.ApplicationSettings.id,
				new LoginParams () {
					id = login.id,
						secret = login.secret
				}));

		yield return new WaitForSeconds (0.5f);

		// register second user
		var receiverUser = CoflnetUser.Generate (ConfigController.ApplicationSettings.id);
		receiverUser.GetCommandController ().OverwriteCommand<ChatMessageCommand> ();

		//register the command
		(new CoflnetUser ()).GetCommandController ().OverwriteCommand<TestCommandWithPermission> ();
		//tell the client what user we are
		ConfigController.ActiveUserId = login.id;
		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<ChatMessageCommand, string> (receiverUser.Id, "hi"));

		yield return new WaitForSeconds (0.5f);

		// message is saved
		Assert.IsTrue (MessagePersistence.ServerInstance.GetMessagesFor (receiverUser.Id).ToList ().Any ());

		// connect with the receiver
		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<Coflnet.LoginUser, LoginParams> (
				ConfigController.ApplicationSettings.id,
				new LoginParams () {
					id = receiverUser.Id,
						secret = receiverUser.Secret
				}));

		yield return new WaitForSeconds (0.5f);
		// tell the Client socket the identity
		ConfigController.ActiveUserId = receiverUser.Id;

		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<Coflnet.ReceiveableResource.GetMessages, int> (receiverUser.Id, 0));

		yield return new WaitForSeconds (0.5f);
		Logger.Log (response.type);
		// message is delivered
		Assert.AreEqual (response.GetAs<string> (), "hi");

		// message is deleted
		Assert.IsFalse (MessagePersistence.ServerInstance.GetMessagesFor (receiverUser.Id).ToList ().Any ());

		// clean up      
		ServerCore.Stop ();
	}

	class ChatMessageCommand : Command {
		public override void Execute (CommandData data) {
			Logger.Log ("executed");
		}

		protected override CommandSettings GetSettings () {
			return new CommandSettings (false, true, true, true, IsNotBockedPermission.Instance);
		}

		public override string Slug => "chatMessage";

	}

	[UnityTest]
	public IEnumerator MessageTransitPersitenceCrossServerTest () {
		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference (1, 1, 1, 0);
		ServerCore.Init ();
		ClientSocket.Instance.Reconnect ();
		CommandData response = null;
		ClientSocket.Instance.AddCallback (data => {
			response = data;
		});
		ConfigController.ActiveUserId = SourceReference.Default;

		yield return new UnityEngine.WaitForSeconds (0.5f);

		// set data
		var request = new RegisterUserRequest ();
		request.captchaToken = "";
		request.clientId = ConfigController.ApplicationSettings.id;

		// send the command
		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<RegisterUser, RegisterUserRequest> (ConfigController.ApplicationSettings.id, request));

		// await response
		yield return new UnityEngine.WaitForSeconds (0.5f);

		var login = response.GetAs<RegisterUserResponse> ();

		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<Coflnet.LoginUser, LoginParams> (
				ConfigController.ApplicationSettings.id,
				new LoginParams () {
					id = login.id,
						secret = login.secret
				}));

		yield return new WaitForSeconds (0.5f);

		// register second user on virtual other server
		var receiverUser = CoflnetUser.Generate (new SourceReference (1, 1, 2, 0));
		receiverUser.Id = new SourceReference (3, 1, 2, ThreadSaveIdGenerator.NextId);
		receiverUser.GetCommandController ().OverwriteCommand<ChatMessageCommand> ();
		Logger.Log (receiverUser.Id);

		(new CoflnetUser ()).GetCommandController ().OverwriteCommand<TestCommandWithPermission> ();

		ClientSocket.Instance.SendCommand (
			CommandData.CreateCommandData<ChatMessageCommand, string> (receiverUser.Id, "hi"));

		yield return new WaitForSeconds (0.5f);

		// message is saved
		Assert.IsTrue (MessagePersistence.ServerInstance.GetMessagesFor (receiverUser.Id).ToList ().Any ());

	}

	#endif
}