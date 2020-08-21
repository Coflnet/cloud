using NUnit.Framework;
using Coflnet.Dev;
using Coflnet;
using System.Runtime.Serialization;

public class CloningTest {

    [Test]
    public void CloningTestSimplePasses() {
        var aliceId = new EntityId(2,3);
        var bobId = new EntityId(5,0);


        DevCore.Init(aliceId,true);

        

        var alice = DevCore.DevInstance.simulationInstances[aliceId.FullServerId].core;
        var bob = DevCore.DevInstance.AddServerCore(bobId).core;

        
        var resource = new TestEntity();
        // register resource on Server2
        resource.AssignId(bob.EntityManager);
        resource.specialNumber = 42;
        // authorize access
        resource.GetAccess().Authorize(aliceId);

        // make sure the resource is there
                Assert.AreEqual(resource.specialNumber,
        bob.EntityManager.GetEntity<TestEntity>(resource.Id).specialNumber);

        
        // clone the resource to server1
        alice.EntityManager.CloneEntity(resource.Id,res=>{
            Assert.AreEqual(42, (res as TestEntity).specialNumber);
        });
    }


        [Test]
    public void CloningCommandBufferTest() {
        var aliceId = new EntityId(2,3);
        var bobId = new EntityId(5,0);


        DevCore.Init(aliceId,true);

        var alice = DevCore.DevInstance.simulationInstances[aliceId.FullServerId].core;
        var bob = DevCore.DevInstance.AddServerCore(bobId).core;

        var resource = new TestEntity();
        // register resource on Server2
        resource.AssignId(bob.EntityManager);
        resource.specialNumber = 42;
        // authorize access to the whole server
        resource.GetAccess().Authorize(aliceId.FullServerId);

        // send command
        alice.SendCommand<SimpleTestCommand,short>(resource.Id,0);


        bool calbackExecuted = false;
        
        // clone the resource to server1
        alice.EntityManager.CloneEntity(resource.Id,res=>{
            calbackExecuted = true;
            Assert.AreEqual(43, (res as TestEntity).specialNumber);
            calbackExecuted = true;
        });

        // the devsocket always executes callback bevore continuing if no exception occured
        Assert.IsTrue(calbackExecuted);
    }




    [Test]
    public void CloneAndSubscribeTest()
    {
        var aliceId = new EntityId(2,3);
        var bobId = new EntityId(5,0);


        DevCore.Init(aliceId,true);

        var alice = DevCore.DevInstance.simulationInstances[aliceId].core;
        var aliceServer = DevCore.DevInstance.simulationInstances[aliceId.FullServerId].core;
        var bob = DevCore.DevInstance.AddServerCore(bobId).core;

         var resource = new TestEntity();
        // register resource on bob
        resource.AssignId(bob.EntityManager);
        resource.specialNumber = 42;
        // authorize access to the whole server
        resource.GetAccess().Authorize(aliceId.FullServerId,AccessMode.WRITE);


        // clone it
        alice.CloneAndSubscribe(resource.Id);



        // send (execute) command
        bob.SendCommand<SimpleTestCommand,short>(resource.Id,0);

        // assert that update was distributed
        Assert.AreEqual(43,aliceServer.EntityManager.GetEntity<TestEntity>(resource.Id).specialNumber);
        Assert.AreEqual(43,alice.EntityManager.GetEntity<TestEntity>(resource.Id).specialNumber);

    }


    [DataContract]
    public class TestEntity : Entity
    {
        [IgnoreDataMember]
        static CommandController commands = new CommandController(globalCommands);

        [DataMember]
        public int specialNumber;

        static TestEntity()
        {
            commands.RegisterCommand<SimpleTestCommand>();
        }



        public override CommandController GetCommandController()
        {
            return commands;
        }
    }

    public class SimpleTestCommand : Command
    {
        public override string Slug => "stcabc";

        public override void Execute(CommandData data)
        {
            var res = data.GetTargetAs<Entity>();
            var access = res.GetAccess();
            foreach (var item in access.GetSpecialCases())
            {
                Logger.Log($"{item.Key} has {item.Value} and {data.GetTargetAs<Entity>().Access.generalAccess}");   
            }
            data.GetTargetAs<TestEntity>().specialNumber++;
        }

        protected override CommandSettings GetSettings()
        {
            return new CommandSettings(false,true,false,false,WritePermission.Instance);
        }
    }


}
