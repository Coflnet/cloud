using NUnit.Framework;
using Coflnet.Dev;
using Coflnet;
using System.Runtime.Serialization;

public class CreationTests {

    [Test]
    public void CreationTestsSimplePasses() {
		var userId = new EntityId(1,1);
		DevCore.Init(userId);

		var user = DevCore.DevInstance.simulationInstances[userId].core as ClientCoreProxy;
		var server = DevCore.DevInstance.simulationInstances[userId.FullServerId].core as ServerCoreProxy;


		Entity.globalCommands.OverwriteCommand<CreateTestCommand>();


		var res = user.CreateEntity<CreateTestCommand>();

		// the resource should now exist both on the client and server instance

		var resource = user.EntityManager.GetEntity<TestResource>(res.Id);

		
		// is the right type
		Assert.IsTrue(resource is TestResource);
		// has a nonlocal serverId
		Assert.IsTrue(resource.Id.ServerId != 0);
		// exists on the server
		Assert.IsTrue(server.EntityManager.Exists(resource.Id));
		
    }

	[Test]
	public void CreationTestsWithOptions() {
		var testValue =50;

		var userId = new EntityId(1,1);
		DevCore.Init(userId);

		var user = DevCore.DevInstance.simulationInstances[userId].core as ClientCoreProxy;


		Entity.globalCommands.OverwriteCommand<CreateTestWithOptionsCommand>();


		var res = user.CreateEntity<CreateTestWithOptionsCommand,CreateTestWithOptionsCommand.Options>(
			new CreateTestWithOptionsCommand.Options(){
				Value=testValue
			});

		// the resource should now exist both on the client and server instance

		var resource = user.EntityManager.GetEntity<TestResource>(res.Id);

		
		Assert.AreEqual(testValue,resource.value);
    }


	[DataContract]
    public class TestResource : Entity
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

        public override Entity CreateEntity(CommandData data)
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

        public override Entity CreateEntity(CommandData data)
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
