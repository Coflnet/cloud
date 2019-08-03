using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Coflnet;
using MessagePack.Resolvers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SerializationTests {
	private static string _TestCommandName = "testCommand";

	[Test]
	public void DependencyTest () {
		var cc = new CommandController ();
		var md = new MessageData ();
		Assert.IsNotNull (cc);
		Assert.IsNotNull (md);
	}

	public class TestCommand : Command {
		public override void Execute (MessageData data) {
			throw new TestException ();
		}

		public override CommandSettings GetSettings () {
			return new CommandSettings ();
		}

		public override string Slug => _TestCommandName;

	}

	public class TestException : CoflnetException {
		public TestException (int responseCode = -1) : base ("test_successful", "this test is expected to throw this exception", null, responseCode) { }
	}

	[Test]
	public void CommandControllerRegister () {
		var cc = new CommandController ();
		cc.RegisterCommand<TestCommand> ();
	}

	[Test]
	public void CommandControllerDoubleRegister () {
		var cc = new CommandController ();
		cc.RegisterCommand<TestCommand> ();
		Assert.Throws<CoflnetException> (() => {
			cc.RegisterCommand<TestCommand> ();
		});
	}

	[Test]
	public void CommandControllerOverwrite () {
		var cc = new CommandController ();
		cc.RegisterCommand<TestCommand> ();
		cc.OverwriteCommand<TestCommand> (_TestCommandName);
	}

	[Test]
	public void CommandControllerBackfall () {
		var cc = new CommandController ();
		cc.RegisterCommand<TestCommand> ();

		var ccOuter = new CommandController (cc);
		Assert.Throws<TestException> (() => {
			ccOuter.ExecuteCommand (new MessageData (_TestCommandName, null));
		});
	}

	[Test]
	public void CommandControllerUnknownCommand () {
		var cc = new CommandController ();
		var ccOuter = new CommandController (cc);
		Assert.Throws<CommandUnknownException> (() => {
			ccOuter.ExecuteCommand (new MessageData (_TestCommandName + "abc", null));
		});
	}

	[Test]
	public void UserCreate () {
		var user1 = new CoflnetUser ();
		var user2 = new CoflnetUser (new SourceReference ());

		Assert.IsNotNull (user1);
		Assert.IsNotNull (user2);
	}

	[Test]
	public void SourceReferenceLocation () {
		var sourceReference = new SourceReference (1, 2, 3, 0);
		UnityEngine.Debug.Log (sourceReference.ServerId);
		Assert.AreEqual (2, sourceReference.LocationInRegion);
	}

	[Test]
	public void SourceReferenceBigLocation () {
		var sourceReference = new SourceReference (1, 1234567, 3, 0);
		Assert.AreEqual (1234567, sourceReference.LocationInRegion);
	}

	[Test]
	public void SourceReferenceRegion () {
		var sourceReference = new SourceReference (1, 2, 3, 0);
		Assert.AreEqual (1, sourceReference.Region);
	}

	[Test]
	public void MultipleObjectWriteTest () {
		FileController.WriteLinesAs<int> ("ok", TestInts ());
		var read = FileController.ReadLinesAs<int> ("ok");
		CollectionAssert.AreEqual(TestInts(),read);
	}

	private IEnumerable<int> TestInts () {
		yield return 0;
		yield return 1;
		yield return 2;
		yield return 3;
		yield return 4;
	}

	long referenceId = 0;

	[Test]
	public void ThreadSafeIdGeneratorTest () {
		long startId = ThreadSaveIdGenerator.NextId;

		var start = new ThreadStart (() => {
			for (int i = 0; i < 500000; i++) {
				var id = ThreadSaveIdGenerator.NextId;
				if (id < referenceId)
					throw new System.Exception ($"The last id was smaller than the one before, thus not unique {id} < {referenceId}");
				else
					referenceId = id;
			}
		});
		var threads = new List<Thread> ();
		for (int i = 0; i < 10; i++) {
			var t = new Thread (start);
			t.Start ();
			threads.Add (t);
		}

		threads[0].Join ();

		Debug.Log ($"start: {startId} end: {ThreadSaveIdGenerator.NextId} ");
	}

	[Test]
	public void ThreadSafeIdGeneratorSingleThreadTest () {
		long id;
		long start = ThreadSaveIdGenerator.NextId;
		long lastId = start;
		Debug.Log ("start: " + start);
		for (int i = 0; i < 10000000; i++) {
			id = ThreadSaveIdGenerator.NextId;
			if (id < lastId)
				throw new System.Exception ($"The last id was smaller than the one before, thus not unique {id} < {lastId} i= {i}");
			lastId = id;
		}

		long end = ThreadSaveIdGenerator.NextId;
		Debug.Log ($"end: {end }  dif {end - start}");
	}

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator SerializationTestsWithEnumeratorPasses () {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
	}

	[Test]
	public void ResourceSave () {

		var user = new CoflnetUser ();
		user.AssignId ();
		user.OnlyFriendsMessage = true;
		user.FirstName = "Bernd das Brot ist";
		DataController.Instance.SaveData ($"res/{user.Id.ToString()}aaaa", user.Serialize ());
		ReferenceManager.Instance.Save (user.Id, true);
	}

	/// <summary>
	/// Resources the save and load and actually useable after loading
	/// </summary>
	[Test]
	public void ResourceSaveAndLoad () {
		//CompositeResolver.RegisterAndSetAsDefault(PrimitiveObjectResolver.Instance, StandardResolver.Instance);

		var user = new CoflnetUser ();
		user.AssignId ();
		user.OnlyFriendsMessage = true;
		user.FirstName = "Bernd das Brot";

		DataController.Instance.SaveData ($"res/{user.Id.ToString()}aaaa", MessagePack.MessagePackSerializer.Typeless.Serialize (user));
		ReferenceManager.Instance.Save (user.Id, true);

		var loadedUser = ReferenceManager.Instance.GetResource<CoflnetUser> (user.Id);
		Assert.AreEqual (user.FirstName, loadedUser.FirstName);
	}
}