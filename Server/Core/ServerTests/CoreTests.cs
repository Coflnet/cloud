﻿using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet;
using Coflnet.Server;
using System;

public class CoreTests
{

	[Test]
	public void CoreTestsSimplePasses()
	{
		// Use the Assert class to test conditions.

	}

	public class TestResource : Referenceable
	{
		static CommandController commands;
		public int value;

		static TestResource()
		{
			commands = new CommandController();
			commands.RegisterCommand<TestCommand>();
		}

		public override CommandController GetCommandController()
		{
			return commands;
		}

		public TestResource() : base(new SourceReference())
		{

		}

		public class TestCommand : Command
		{
			public override void Execute(MessageData data)
			{
				data.GetResource<TestResource>().value = 5;
			}

			public override CommandSettings GetSettings()
			{
				return new CommandSettings();
			}

			public override string GetSlug()
			{
				return "coreTest";
			}
		}
		public class ReturnTestCommand : Coflnet.ServerCommand
		{
			public override void Execute(MessageData data)
			{
				SendBack(data, data.GetResource<TestResource>().value);
			}

			public override ServerCommandSettings GetServerSettings()
			{
				return new ServerCommandSettings();
			}

			public override string GetSlug()
			{
				return "returnTest";
			}
		}
	}


	[Test]
	public void CreateResourceAndAddAccess()
	{
		Referenceable referenceable = new TestResource();
		referenceable.Access = new Access(new SourceReference());
	}

	[Test]
	public void CreateAccessAndSetOwner()
	{
		var access = new Access(new SourceReference());
		access.Owner = new SourceReference(12, 23);
	}

	[Test]
	public void AccessTestOwner()
	{
		var access = new Access(new SourceReference());
		access.Owner = new SourceReference(12, 23);

		Assert.IsTrue(access.IsAllowedToAccess(new SourceReference(12, 23)));
	}


	[Test]
	public void AccessTestCustom()
	{
		var access = new Access(new SourceReference());
		access.Owner = new SourceReference(12, 23);

		Assert.IsTrue(access.IsAllowedToAccess(new SourceReference(12, 23)));
	}

	[Test]
	public void StoreResourceInManager()
	{
		var res = new TestResource();

		var id = ReferenceManager.Instance.CreateReference(res);

		Assert.IsTrue(ReferenceManager.Instance.Contains(id));
	}

	[Test]
	public void GetResourceInManager()
	{
		var res = new TestResource();
		res.value = 1111;
		var id = ReferenceManager.Instance.CreateReference(res);
		var retrivedRes = ReferenceManager.Instance.GetResource(id);
		Assert.IsTrue(retrivedRes.Id == res.Id);
	}

	[Test]
	public void GetResourceInManagerGeneric()
	{
		var res = new TestResource();
		res.value = 1111;
		var id = ReferenceManager.Instance.CreateReference(res);
		var retrivedRes = ReferenceManager.Instance.GetResource<TestResource>(id);
		Assert.IsTrue(retrivedRes.value == 1111);
	}

	[Test]
	public void RecourceOverridePreventTest()
	{
		var res = new TestResource();
		res.Id = new SourceReference(5, 2);
		Assert.Throws<Exception>(() =>
		{
			var id = ReferenceManager.Instance.CreateReference(res);
		});
	}

	[Test]
	public void RecourceOverridePreventForceTest()
	{
		var res = new TestResource();
		res.Id = new SourceReference(5, 2);
		var id = ReferenceManager.Instance.CreateReference(res, true);
	}

	[Test]
	public void RecourceCommandTest()
	{
		var res = new TestResource();
		var id = ReferenceManager.Instance.CreateReference(res);
		var retrivedRes = ReferenceManager.Instance.GetResource<TestResource>(id);

		retrivedRes.GetCommandController().ExecuteCommand(new MessageData(id, null, "coreTest"));

		Assert.IsTrue(res.value == 5);
	}


	[UnityTest]
	public IEnumerator ConnectingTest()
	{
		ServerCore.Init();
		ClientSocket.Instance.AddCallback(data =>
		{

		});


		yield return new UnityEngine.WaitForSeconds(1);


		ClientSocket.Instance.SendCommand(new MessageData());

		//retrivedRes.GetCommandController().ExecuteCommand(new MessageData(id, null, "coreTest"));

		//Assert.IsTrue(res.value == 5);
	}


	[UnityTest]
	public IEnumerator ErrorResponseTest()
	{
		ServerCore.Init();
		string returnValue = null;
		ClientSocket.Instance.OnError +=
			(CoflnetException coflnetException)
						=>
			{
				returnValue = coflnetException.Slug;
			};



		yield return new UnityEngine.WaitForSeconds(1);

		// the error is whitelisted
		// beause the console is also the server console this would cause the test to faild otherwise
		LogAssert.Expect(UnityEngine.LogType.Error,
						 new System.Text.RegularExpressions.Regex(".*The requested object doesn't exist on this server.*"));

		ClientSocket.Instance.SendCommand(new MessageData(new SourceReference(1, 1, 1, ThreadSaveIdGenerator.NextId)));

		yield return new UnityEngine.WaitForSeconds(1);


		// test the expected error slug
		Assert.AreEqual(returnValue, "object_not_found");

		//retrivedRes.GetCommandController().ExecuteCommand(new MessageData(id, null, "coreTest"));
	}

	class ServerTestCommandGet : Coflnet.ServerCommand
	{
		public override void Execute(MessageData data)
		{
			SendBack(data, 5);
		}

		public override ServerCommandSettings GetServerSettings()
		{
			return new ServerCommandSettings();
		}

		public override string GetSlug()
		{
			return "serverTestCommandGet";
		}
	}


	[UnityTest]
	public IEnumerator CommandResponseTest()
	{

		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference(1, 1, 1, 0);
		ServerCore.Init();
		MessageData response = null;
		ClientSocket.Instance.AddCallback(data =>
		{
			response = data;
		});


		// register server command
		ServerCore.Commands.RegisterCommand<ServerTestCommandGet>();

		yield return new UnityEngine.WaitForSeconds(1);

		// send the command
		ClientSocket.Instance.SendCommand(
			new MessageData(ConfigController.ApplicationSettings.id, -1, "", "serverTestCommandGet"));

		// await response
		yield return new UnityEngine.WaitForSeconds(1);


		// test the expected error slug
		Assert.AreEqual(BitConverter.ToInt32(response.message, 0), 4);

		//retrivedRes.GetCommandController().ExecuteCommand(new MessageData(id, null, "coreTest"));
	}



	[Test]
	public void MessagePersistenceTest()
	{

		var data = new MessageData(new SourceReference(0, 1, 2, 3),
								   System.Text.Encoding.UTF8.GetBytes("hi, I am a long text to get over 64 bytes and trigger the lz4 compresseion :)"),
								   "testCommand");

		MessagePersistence.Instance.DeleteMessages(data.rId);
		MessagePersistence.Instance.Save(data);


		foreach (var item in MessagePersistence.Instance.MessagesFor(data.rId))
		{
			Assert.AreEqual(data, item);
		}
	}



	[Test]
	public void MessagePersistenceTestMultiple()
	{
		var data = new MessageData(new SourceReference(0, 1, 2, 3),
								   System.Text.Encoding.UTF8.GetBytes("hi, I am a string that will be a byte array when it grows up"),
								   "testCommand");

		MessagePersistence.Instance.DeleteMessages(data.rId);
		MessagePersistence.Instance.Save(data);
		MessagePersistence.Instance.Save(data);
		MessagePersistence.Instance.Save(data);
		MessagePersistence.Instance.Save(data);


		foreach (var item in MessagePersistence.Instance.MessagesFor(data.rId))
		{
			Assert.AreEqual(data, item);
		}
	}


	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator CoreTestsWithEnumeratorPasses()
	{
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return new UnityEngine.WaitForSeconds(1);
	}
}