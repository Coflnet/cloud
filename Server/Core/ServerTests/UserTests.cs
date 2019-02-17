using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet.Server;
using Coflnet;

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
}
