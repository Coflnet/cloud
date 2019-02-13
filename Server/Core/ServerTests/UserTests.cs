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

		// send the command
		ClientSocket.Instance.SendCommand(
			new MessageData(ConfigController.ApplicationSettings.id, -1, "", "registerUser"));

		// await response
		yield return new UnityEngine.WaitForSeconds(1);
		Debug.Log(response.ToString());
		Assert.AreEqual(response.GetAs<int>(), 4);

		//retrivedRes.GetCommandController().ExecuteCommand(new MessageData(id, null, "coreTest"));
	}
}
