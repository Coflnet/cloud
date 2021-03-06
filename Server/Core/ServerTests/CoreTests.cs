﻿using System;
using System.Collections;
using System.Text.RegularExpressions;
using Coflnet;
using Coflnet.Server;
using NUnit.Framework;

public class CoreTests {

	[Test]
	public void CoreTestsSimplePasses () {
		// Use the Assert class to test conditions.

	}

	public class TestResource : Entity {
		static CommandController commands;
		public int value;

		static TestResource () {
			commands = new CommandController ();
			commands.RegisterCommand<TestCommand> ();
		}

		public override CommandController GetCommandController () {
			return commands;
		}

		public TestResource () : base (new EntityId ()) {

		}

		public class TestCommand : Command {
			public override void Execute (CommandData data) {
				data.GetEntity<TestResource> ().value = 5;
			}

			protected override CommandSettings GetSettings () {
				return new CommandSettings ();
			}

			public override string Slug => "coreTest";

		}
		public class ReturnTestCommand : Coflnet.ServerCommand {
			public override void Execute (CommandData data) {
				SendBack (data, data.GetEntity<TestResource> ().value);
			}

			public override ServerCommandSettings GetServerSettings () {
				return new ServerCommandSettings ();
			}

			public override string Slug => "returnTest";

		}
	}

	[Test]
	public void CreateResourceAndAddAccess () {
		Entity entity = new TestResource ();
		entity.Access = new Access (new EntityId ());
	}

	[Test]
	public void CreateAccessAndSetOwner () {
		var access = new Access (new EntityId ());
		access.Owner = new EntityId (12, 23);
	}

	[Test]
	public void AccessTestOwner () {
		var access = new Access (new EntityId ());
		access.Owner = new EntityId (12, 23);

		Assert.IsTrue (access.IsAllowedToAccess (new EntityId (12, 23)));
	}

	[Test]
	public void AccessTestCustom () {
		var access = new Access (new EntityId ());
		access.Owner = new EntityId (12, 23);

		Assert.IsTrue (access.IsAllowedToAccess (new EntityId (12, 23)));
	}

	[Test]
	public void StoreResourceInManager () {
		var res = new TestResource ();
		res.AssignId ();
		Assert.IsTrue (EntityManager.Instance.Exists (res.Id));
	}

	[Test]
	public void GetResourceInManager () {
		var res = new TestResource ();
		res.AssignId ();
		res.value = 1111;
		var retrivedRes = EntityManager.Instance.GetResource (res.Id);
		Assert.IsTrue (retrivedRes.Id == res.Id);
	}

	[Test]
	public void GetResourceInManagerGeneric () {
		var res = new TestResource ();
		res.AssignId ();
		res.value = 1111;
		var retrivedRes = EntityManager.Instance.GetEntity<TestResource> (res.Id);
		Assert.IsTrue (retrivedRes.value == 1111);
	}

	[Test]
	public void RecourceOverridePreventTest () {
		var res = new TestResource ();
		res.Id = new EntityId (5, 2);
		Assert.Throws<Exception> (() => {
			EntityManager.Instance.CreateReference (res);
		});
	}

	[Test]
	public void RecourceOverridePreventForceTest () {
		var res = new TestResource ();
		res.Id = new EntityId (5, 2);
		var id = EntityManager.Instance.CreateReference (res, true);
		Assert.AreNotEqual (id, new EntityId ());
	}

	[Test]
	public void RecourceCommandTest () {
		var res = new TestResource ();
		res.AssignId ();
		var id = res.Id;
		var retrivedRes = EntityManager.Instance.GetEntity<TestResource> (id);

		retrivedRes.GetCommandController ().ExecuteCommand (new CommandData (id, null, "coreTest"));

		Assert.IsTrue (res.value == 5);
	}

	/// <summary>
	/// Tests the Server core initialization process.
	/// </summary>
	[Test]
	public void ServerCoreInit () {
		ServerCore.Init ();
	}

#if UnityEngine
	[UnityTest]
	public IEnumerator ConnectingTest () {
		ServerCore.Init ();
		ClientSocket.Instance.AddCallback (data => {

		});
		// p
		ClientSocket.Instance.Reconnect ();
		yield return new UnityEngine.WaitForSeconds (0.5f);

		LogAssert.Expect (UnityEngine.LogType.Error,
			new Regex ($".*There is no server with the id 0.*"));
		ClientSocket.Instance.SendCommand (new CommandData ());

		//retrivedRes.GetCommandController().ExecuteCommand(new CommandData(id, null, "coreTest"));

		yield return new UnityEngine.WaitForSeconds (0.5f);
		ServerCore.Stop ();
		//Assert.IsTrue(res.value == 5);
	}

	[UnityTest]
	public IEnumerator ErrorResponseTest () {
		// Tell the server its id (so he doesn't try to pass the message on)
		ConfigController.ApplicationSettings.id = new SourceReference (1, 1, 1, 0);
		ServerCore.Init ();
		string returnValue = null;
		ClientSocket.Instance.OnError +=
			(CoflnetException coflnetException) => {
				returnValue = coflnetException.Slug;
			};

		ClientSocket.Instance.Reconnect ();
		yield return new UnityEngine.WaitForSeconds (1);

		var newId = new SourceReference (1, 1, 1, ThreadSaveIdGenerator.NextId);

		// the error is whitelisted
		// beause the console is also the server console this would cause the test to faild otherwise
		LogAssert.Expect (UnityEngine.LogType.Error,
			new Regex ($".*{Regex.Escape(newId.ToString())}.*wasn't found on this server.*"));

		ClientSocket.Instance.SendCommand (new CommandData (newId), false);

		yield return new UnityEngine.WaitForSeconds (1);

		// test the expected error slug
		Assert.AreEqual (returnValue, "object_not_found");

		ServerCore.Stop ();
		//retrivedRes.GetCommandController().ExecuteCommand(new CommandData(id, null, "coreTest"));
	}

	[UnityTest]
	public IEnumerator CommandResponseTest () {

		// tell the server his id
		ConfigController.ApplicationSettings.id = new SourceReference (1, 1, 1, 0);
		ServerCore.Init ();
		CommandData response = null;
		ClientSocket.Instance.Reconnect ();
		ClientSocket.Instance.AddCallback (data => {
			response = data;
		});
		ConfigController.ActiveUserId = SourceReference.Default;

		// register server command
		ServerCore.Commands.RegisterCommand<ServerTestCommandGet> ();

		yield return new UnityEngine.WaitForSeconds (1);

		// send the command
		ClientSocket.Instance.SendCommand (
			new CommandData (ConfigController.ApplicationSettings.id, -1, "", "serverTestCommandGet"));

		// await response
		yield return new UnityEngine.WaitForSeconds (1);

		// test the expected error slug
		Assert.AreEqual (response.GetAs<int> (), 4);

		ServerCore.Stop ();
	}

#endif

	class ServerTestCommandGet : Coflnet.ServerCommand {
		public override void Execute (CommandData data) {
			//SendBack(data, 5);
			data.SerializeAndSet<int> (4);
			data.SendBack (data);
		}

		public override ServerCommandSettings GetServerSettings () {
			return new ServerCommandSettings ();
		}

		public override string Slug => "serverTestCommandGet";

	}

	[Test]
	public void MessagePersistenceTest () {

		var data = new CommandData (new EntityId (0, 1, 2, 3),
			System.Text.Encoding.UTF8.GetBytes ("hi, I am a long text to get over 64 bytes and trigger the lz4 compresseion :)"),
			"testCommand");
		// manually assing an id, is usaly done in the sending process
		data.MessageId = ThreadSaveIdGenerator.NextId;

		MessagePersistence.ServerInstance.DeleteMessages (data.Recipient);
		MessagePersistence.ServerInstance.SaveMessage (data);

		foreach (var item in MessagePersistence.ServerInstance.GetMessagesFor (data.Recipient)) {
			Assert.AreEqual (data, item);
		}
	}

	[Test]
	public void MessagePersistenceTestMultiple () {
		var data = new CommandData (new EntityId (0, 1, 2, 3),
			System.Text.Encoding.UTF8.GetBytes ("hi, I am a string that will be a byte array when it grows up"),
			"testCommand");

		// manually assing an id, is usaly done in the sending process
		data.MessageId = ThreadSaveIdGenerator.NextId;

		// reset
		MessagePersistence.ServerInstance.DeleteMessages (data.Recipient);
		// save
		MessagePersistence.ServerInstance.SaveMessage (data);
		MessagePersistence.ServerInstance.SaveMessage (data);
		MessagePersistence.ServerInstance.SaveMessage (data);
		MessagePersistence.ServerInstance.SaveMessage (data);

		foreach (var item in MessagePersistence.ServerInstance.GetMessagesFor (data.Recipient)) {
			Assert.AreEqual (data, item);
		}
	}


	[Test]
	public void CommandExtention () {
		// loads the extention
		ServerCore.Init ();

		var loginUser = ServerCore.Commands.GetCommand ("loginUser");
		Assert.IsNotNull (loginUser);

		ServerCore.Stop ();
	}
}