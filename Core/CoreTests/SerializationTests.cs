﻿using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Coflnet;
using MessagePack;
using MessagePack.Resolvers;
using NUnit.Framework;

public class SerializationTests {
	private static string _TestCommandName = "testCommand";

	[Test]
	public void DependencyTest () {
		var cc = new CommandController ();
		var md = new CommandData ();
		Assert.IsNotNull (cc);
		Assert.IsNotNull (md);
	}

	public class TestCommand : Command {
		public override void Execute (CommandData data) {
			throw new TestException ();
		}

		protected override CommandSettings GetSettings () {
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
	public void CommandControllerFallback () {
		var cc = new CommandController ();
		cc.RegisterCommand<TestCommand> ();

		var ccOuter = new CommandController (cc);
		Assert.Throws<TestException> (() => {
			ccOuter.ExecuteCommand (new CommandData (_TestCommandName));
		});
	}

	[Test]
	public void CommandControllerUnknownCommand () {
		var cc = new CommandController ();
		var ccOuter = new CommandController (cc);
		Assert.Throws<CommandUnknownException> (() => {
			ccOuter.ExecuteCommand (new CommandData (_TestCommandName + "abc"));
		});
	}

	[Test]
	public void UserCreate () {
		var user1 = new CoflnetUser ();
		var user2 = new CoflnetUser (new EntityId ());

		Assert.IsNotNull (user1);
		Assert.IsNotNull (user2);
	}

	[Test]
	public void SourceReferenceLocation () {
		var sourceReference = new EntityId (1, 2, 3, 0);
		Assert.AreEqual (2, sourceReference.LocationInRegion);
	}

	[Test]
	public void SourceReferenceBigLocation () {
		var sourceReference = new EntityId (1, 1234567, 3, 0);
		Assert.AreEqual (1234567, sourceReference.LocationInRegion);
	}

	[Test]
	public void SourceReferenceRegion () {
		var sourceReference = new EntityId (1, 2, 3, 0);
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

	/// <summary>
	/// Consumes ~50MB RAM for generated ids
	/// </summary>
	[Test]
	public void ThreadSafeIdGeneratorTest () {
		var ids = new ConcurrentDictionary<long,byte>(); 
		var threadCount = 10;
		var iterationsPerThread = 500000;
		long lastId = 0 ; 
		var start = new ThreadStart (() => {
			for (int i = 0; i < iterationsPerThread; i++) {
				var id = ThreadSaveIdGenerator.NextId;
				
				if (!ids.TryAdd(id,0))
					throw new System.Exception ($"The last id was already generated once {id} ");

			}
			if(threadCount*iterationsPerThread == ids.Count)
			{
				// done
				lastId = ThreadSaveIdGenerator.NextId;
			}
		});
		var threads = new List<Thread> ();
		var firstId = ThreadSaveIdGenerator.NextId;
		for (int i = 0; i < 10; i++) {
			var t = new Thread (start);
			t.Start ();
			threads.Add (t);
		}


		foreach (var thread in threads)
		{
			thread.Join();
		}
		threads.Clear();

		ids.Clear();
	}

	[Test]
	public void ThreadSafeIdGeneratorSingleThreadTest () {
		long id;
		long start = ThreadSaveIdGenerator.NextId;
		long lastId = start;
		for (int i = 0; i < 10000000; i++) {
			id = ThreadSaveIdGenerator.NextId;
			if (id < lastId)
				throw new System.Exception ($"The last id was smaller than the one before, thus not unique {id} < {lastId} i= {i}");
			lastId = id;
		}

		long end = ThreadSaveIdGenerator.NextId;
	}


	[Test]
	public void ResourceSave () {
		var resolver = CompositeResolver.Create(PrimitiveObjectResolver.Instance, StandardResolver.Instance);
		var user = new CoflnetUser ();
		user.AssignId ();
		user.OnlyFriendsMessage = true;
		user.FirstName = "Bernd das Brot ist";
		DataController.Instance.SaveData ($"res/{user.Id.ToString()}aaaa", MessagePackSerializer.Typeless.Serialize(user));
		EntityManager.Instance.Save (user.Id, true);
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
		EntityManager.Instance.Save (user.Id, true);

		var loadedUser = EntityManager.Instance.GetEntity<CoflnetUser> (user.Id);
		Assert.AreEqual (user.FirstName, loadedUser.FirstName);
	}
}