using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet.Server;
using Coflnet;
using System.Linq;

public class CoflnetUserTests
{

	[Test]
	public void RegisterUserCommand()
	{
		ServerCore.Init();

	}


	[Test]
	public void UserMessageForwarding()
	{
		ServerCore.Init();

	}

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator CreateUserTest()
	{
		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference(1, 1, 1, 0);
		ServerCore.Init();
		MessageData response = null;
		ClientSocket.Instance.AddCallback(data =>
		{
			response = data;
		});


		yield return new UnityEngine.WaitForSeconds(1);

		// set data
		var request = new RegisterUserRequest();
		request.captchaToken = "";
		request.clientId = ConfigController.ApplicationSettings.id;

		// send the command
		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<RegisterUser, RegisterUserRequest>(request, ConfigController.ApplicationSettings.id));

		// await response
		yield return new UnityEngine.WaitForSeconds(0.5f);
		Debug.Log(response.GetAs<RegisterUserResponse>().id);

		// user was created in the last second
		Assert.IsTrue(response.GetAs<RegisterUserResponse>().id.ResourceId > ThreadSaveIdGenerator.NextId - 10000000);
	}



	public class TestCommandWithPermission : Command
	{
		public override void Execute(MessageData data)
		{
			data.SendBack(MessageData.SerializeMessageData(5, "success"));
		}

		public override CommandSettings GetSettings()
		{
			return new CommandSettings(false, false, false, IsAuthenticatedPermission.Instance);
		}

		public override string GetSlug()
		{
			return "testCommandPermission";
		}
	}



	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator CreateAndLoginUserTest()
	{
		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference(1, 1, 1, 0);
		ServerCore.Init();
		MessageData response = null;
		ClientSocket.Instance.AddCallback(data =>
		{
			response = data;
		});



		yield return new UnityEngine.WaitForSeconds(1);

		// set data
		var request = new RegisterUserRequest();
		request.captchaToken = "";
		request.clientId = ConfigController.ApplicationSettings.id;

		// send the command
		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<RegisterUser, RegisterUserRequest>(request, ConfigController.ApplicationSettings.id));

		// await response
		yield return new UnityEngine.WaitForSeconds(0.5f);


		var login = response.GetAs<RegisterUserResponse>();

		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<Coflnet.LoginUser, LoginParams>(new LoginParams()
			{
				id = login.id,
				secret = login.secret
			}, ConfigController.ApplicationSettings.id));

		yield return new WaitForSeconds(0.5f);

		Debug.Log(response.GetAs<RegisterUserResponse>().id);



		// register test command on user
		(new CoflnetUser()).GetCommandController().RegisterCommand<TestCommandWithPermission>();

		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<TestCommandWithPermission, int>(1, login.id));


		yield return new WaitForSeconds(0.5f);


		// user was created in the last second
		Assert.IsTrue(response.GetAs<int>() == 5);
	}



	/// <summary>
	///Tries to send a message to an offline user and tests message persistence
	/// </summary>
	[UnityTest]
	public IEnumerator MessageTransitPersitenceTest()
	{
		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference(1, 1, 1, 0);
		ServerCore.Init();
		MessageData response = null;
		ClientSocket.Instance.AddCallback(data =>
		{
			response = data;
		});



		yield return new UnityEngine.WaitForSeconds(0.5f);

		// set data
		var request = new RegisterUserRequest();
		request.captchaToken = "";
		request.clientId = ConfigController.ApplicationSettings.id;

		// send the command
		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<RegisterUser, RegisterUserRequest>(request, ConfigController.ApplicationSettings.id));

		// await response
		yield return new UnityEngine.WaitForSeconds(0.5f);


		var login = response.GetAs<RegisterUserResponse>();

		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<Coflnet.LoginUser, LoginParams>(new LoginParams()
			{
				id = login.id,
				secret = login.secret
			}, ConfigController.ApplicationSettings.id));

		yield return new WaitForSeconds(0.5f);



		// register second user
		var receiverUser = CoflnetUser.Generate(ConfigController.ApplicationSettings.id);
		receiverUser.GetCommandController().RegisterCommand<ChatMessage>();


		(new CoflnetUser()).GetCommandController().RegisterCommand<TestCommandWithPermission>();

		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<ChatMessage, string>("hi", receiverUser.Id));


		yield return new WaitForSeconds(0.5f);


		// message is saved
		Assert.IsTrue(MessagePersistence.ServerInstance.GetMessagesFor(receiverUser.Id).ToList().Any());



		// connect with the receiver
		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<Coflnet.LoginUser, LoginParams>(new LoginParams()
			{
				id = receiverUser.Id,
				secret = receiverUser.Secret
			}, ConfigController.ApplicationSettings.id));

		yield return new WaitForSeconds(0.5f);
		// tell the Client socket the identity
		ConfigController.UserSettings.userId = receiverUser.Id;


		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<Coflnet.ReceiveableResource.GetMessages, int>(0, receiverUser.Id));



		yield return new WaitForSeconds(0.5f);

		// message is delivered
		Assert.AreEqual(response.GetAs<string>(), "hi");


		// message is deleted
		Assert.IsFalse(MessagePersistence.ServerInstance.GetMessagesFor(receiverUser.Id).ToList().Any());


		// clean up      
		ServerCore.Stop();
	}


	class ChatMessage : Command
	{
		public override void Execute(MessageData data)
		{
			Debug.Log("executed");
		}

		public override CommandSettings GetSettings()
		{
			return new CommandSettings(IsNotBockedPermission.Instance);
		}

		public override string GetSlug()
		{
			return "chatMessage";
		}
	}









	[UnityTest]
	public IEnumerator MessageTransitPersitenceCrossServerTest()
	{
		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference(1, 1, 1, 0);
		ServerCore.Init();
		MessageData response = null;
		ClientSocket.Instance.AddCallback(data =>
		{
			response = data;
		});



		yield return new UnityEngine.WaitForSeconds(0.5f);

		// set data
		var request = new RegisterUserRequest();
		request.captchaToken = "";
		request.clientId = ConfigController.ApplicationSettings.id;

		// send the command
		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<RegisterUser, RegisterUserRequest>(request, ConfigController.ApplicationSettings.id));

		// await response
		yield return new UnityEngine.WaitForSeconds(0.5f);


		var login = response.GetAs<RegisterUserResponse>();

		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<Coflnet.LoginUser, LoginParams>(new LoginParams()
			{
				id = login.id,
				secret = login.secret
			}, ConfigController.ApplicationSettings.id));

		yield return new WaitForSeconds(0.5f);




		// register second user on virtual other server
		var receiverUser = CoflnetUser.Generate(new SourceReference(1, 1, 2, 0));
		receiverUser.Id = new SourceReference(3, 1, 2, ThreadSaveIdGenerator.NextId);
		receiverUser.GetCommandController().RegisterCommand<ChatMessage>();
		Debug.Log(receiverUser.Id);


		(new CoflnetUser()).GetCommandController().RegisterCommand<TestCommandWithPermission>();

		ClientSocket.Instance.SendCommand(
			MessageData.CreateMessageData<ChatMessage, string>("hi", receiverUser.Id));


		yield return new WaitForSeconds(0.5f);


		// message is saved
		Assert.IsTrue(MessagePersistence.ServerInstance.GetMessagesFor(receiverUser.Id).ToList().Any());

	}
}
