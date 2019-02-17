using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet.Server;

public class NetworkingTests
{

	[Test]
	public void NetworkingTestsSimplePasses()
	{
		// Use the Assert class to test conditions.
	}

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator NetworkingWithTwoServersReplication()
	{


		var secondServer = new ServerCore();


		yield return null;
	}
}
