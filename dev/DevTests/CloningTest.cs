﻿using NUnit.Framework;
using Coflnet.Dev;
using Coflnet;
using System.Runtime.Serialization;

public class CloningTest {

    [Test]
    public void CloningTestSimplePasses() {
        var aliceId = new SourceReference(2,3);
        var bobId = new SourceReference(5,0);


        DevCore.Init(aliceId,true);

        

        var alice = DevCore.DevInstance.simulationInstances[aliceId.FullServerId].core;
        var bob = DevCore.DevInstance.AddServerCore(bobId).core;

        
        var resource = new TestResource();
        // register resource on Server2
        resource.AssignId(bob.ReferenceManager);
        resource.specialNumber = 42;
        // authorize access
        resource.GetAccess().Authorize(aliceId);

        // make sure the resource is there
                Assert.AreEqual(resource.specialNumber,
        bob.ReferenceManager.GetResource<TestResource>(resource.Id).specialNumber);

        
        // clone the resource to server1
        alice.ReferenceManager.CloneResource(resource.Id,res=>{
            Assert.AreEqual(42, (res as TestResource).specialNumber);
        });
    }


        [Test]
    public void CloningCommandBufferTest() {
        var aliceId = new SourceReference(2,3);
        var bobId = new SourceReference(5,0);


        DevCore.Init(aliceId,true);

        var alice = DevCore.DevInstance.simulationInstances[aliceId.FullServerId].core;
        var bob = DevCore.DevInstance.AddServerCore(bobId).core;

        var resource = new TestResource();
        // register resource on Server2
        resource.AssignId(bob.ReferenceManager);
        resource.specialNumber = 42;
        // authorize access to the whole server
        resource.GetAccess().Authorize(aliceId.FullServerId);

        // send command
        alice.SendCommand<SimpleTestCommand,short>(resource.Id,0);


        bool calbackExecuted = false;
        
        // clone the resource to server1
        alice.ReferenceManager.CloneResource(resource.Id,res=>{
            calbackExecuted = true;
            Assert.AreEqual(43, (res as TestResource).specialNumber);
            calbackExecuted = true;
        });

        // the devsocket always executes callback bevore continuing if no exception occured
        Assert.IsTrue(calbackExecuted);
    }




    [Test]
    public void CloneAndSubscribeTest()
    {
        var aliceId = new SourceReference(2,3);
        var bobId = new SourceReference(5,0);


        DevCore.Init(aliceId,true);

        var alice = DevCore.DevInstance.simulationInstances[aliceId].core;
        var aliceServer = DevCore.DevInstance.simulationInstances[aliceId.FullServerId].core;
        var bob = DevCore.DevInstance.AddServerCore(bobId).core;

         var resource = new TestResource();
        // register resource on bob
        resource.AssignId(bob.ReferenceManager);
        resource.specialNumber = 42;
        // authorize access to the whole server
        resource.GetAccess().Authorize(aliceId.FullServerId,AccessMode.WRITE);


        // clone it
        alice.CloneAndSubscribe(resource.Id);



        // send (execute) command
        bob.SendCommand<SimpleTestCommand,short>(resource.Id,0);

        // assert that update was distributed
        Assert.AreEqual(43,aliceServer.ReferenceManager.GetResource<TestResource>(resource.Id).specialNumber);
        Assert.AreEqual(43,alice.ReferenceManager.GetResource<TestResource>(resource.Id).specialNumber);

    }


    [DataContract]
    public class TestResource : Referenceable
    {
        [IgnoreDataMember]
        static CommandController commands = new CommandController(globalCommands);

        [DataMember]
        public int specialNumber;

        static TestResource()
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

        public override void Execute(MessageData data)
        {
            var res = data.GetTargetAs<Referenceable>();
            var access = res.GetAccess();
            foreach (var item in access.GetSpecialCases())
            {
                Logger.Log($"{item.Key} has {item.Value} and {data.GetTargetAs<Referenceable>().Access.generalAccess}");   
            }
            data.GetTargetAs<TestResource>().specialNumber++;
        }

        protected override CommandSettings GetSettings()
        {
            return new CommandSettings(false,true,false,false,WritePermission.Instance);
        }
    }


}
