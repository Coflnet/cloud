using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet.Dev;
using Coflnet;
using Coflnet.Client;
using System.Runtime.Serialization;

public class CreationTests {

    [Test]
    public void CreationTestsSimplePasses() {
		var userId = new SourceReference(1,1);
		DevCore.Init(userId);

		var user = DevCore.DevInstance.simulationInstances[userId].core as ClientCoreProxy;
		var server = DevCore.DevInstance.simulationInstances[userId.FullServerId].core as ServerCoreProxy;


		Referenceable.globalCommands.OverwriteCommand<CreateTestCommand>();


		var res = user.CreateResource<CreateTestCommand>();

		// the resource should now exist both on the client and server instance

		var resource = user.ReferenceManager.GetResource<TestResource>(res.Id);

		
		// is the right type
		Assert.IsTrue(resource is TestResource);
		// has a nonlocal serverId
		Assert.IsTrue(resource.Id.ServerId != 0);
		// exists on the server
		Assert.IsTrue(server.ReferenceManager.Exists(resource.Id));
		
    }

	[Test]
	public void CreationTestsWithOptions() {
		var testValue =50;

		var userId = new SourceReference(1,1);
		DevCore.Init(userId);

		var user = DevCore.DevInstance.simulationInstances[userId].core as ClientCoreProxy;


		Referenceable.globalCommands.OverwriteCommand<CreateTestWithOptionsCommand>();


		var res = user.CreateResource<CreateTestWithOptionsCommand,CreateTestWithOptionsCommand.Options>(
			new CreateTestWithOptionsCommand.Options(){
				Value=testValue
			});

		// the resource should now exist both on the client and server instance

		var resource = user.ReferenceManager.GetResource<TestResource>(res.Id);

		
		Assert.AreEqual(testValue,resource.value);
    }


	[DataContract]
    public class TestResource : Referenceable
    {
		[DataMember]
		public int value;

        public override CommandController GetCommandController()
        {
            return globalCommands;
        }
    }

    public class CreateTestCommand : CreationCommand
    {
        public override string Slug => "createtestb";

        public override Referenceable CreateResource(MessageData data)
        {
            return new TestResource();
        }

        protected override CommandSettings GetSettings()
        {
            return new CommandSettings();
        }
    }

	public class CreateTestWithOptionsCommand : CreationCommand
    {
        public override string Slug => "createtestp";

        public override Referenceable CreateResource(MessageData data)
        {
			var options =data.GetAs<Options>();
            return new TestResource(){value=options.Value};
        }

        protected override CommandSettings GetSettings()
        {
            return new CommandSettings();
        }
    
		[MessagePack.MessagePackObject]
		public class Options : CreationParamsBase
		{
			[MessagePack.Key(1)]
			public int Value;
		}
	}
	
}
