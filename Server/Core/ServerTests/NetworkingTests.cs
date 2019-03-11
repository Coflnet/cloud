using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet;
using Coflnet.Server;


public class NetworkingTests
{

	[Test]
	public void DistributionsTest()
	{


	}

	/// <summary>
	/// Tests a distributable resource
	/// </summary>
	/// <returns>The test.</returns>
	[UnityTest]
	public IEnumerator DistributionTest()
	{
		ConfigController.ApplicationSettings.id = new SourceReference(1, 1, 1, 0);
		ServerCore.Init();
		string returnValue = null;
		ClientSocket.Instance.OnError +=
			(CoflnetException coflnetException)
						=>
			{
				returnValue = coflnetException.Slug;
			};

		var secondClient = ClientSocket.NewInstance();
		//secondClient.SendCommand(MessageData.CreateMessageData<Login>);


		ClientSocket.Instance.Reconnect();
		yield return new UnityEngine.WaitForSeconds(1);

		var newId = new SourceReference(1, 1, 1, ThreadSaveIdGenerator.NextId);

		// Ignore log message
		LogAssert.ignoreFailingMessages = true;


		ClientSocket.Instance.SendCommand(new MessageData(newId), false);

		yield return new UnityEngine.WaitForSeconds(1);


		// test the expected error slug
		Assert.AreEqual(returnValue, "object_not_found");

		// Reset
		ServerCore.Stop();
		LogAssert.ignoreFailingMessages = false;
	}
}
