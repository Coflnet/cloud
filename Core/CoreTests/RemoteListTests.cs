using NUnit.Framework;
using System.Collections;
using Coflnet;
using MessagePack;
using System;

public partial class RemoteListTests {

    /// <summary>
    /// Remote list add
    /// </summary>
    [Test]
    public void RemoteListAdd() {
        var owner = new EntityId(5,3);

        DummyCore.Init(owner);

        // create resource
        var list = new ResWithList(owner);
        // register it
        list.AssignId(DummyCore.Instance.EntityManager);

        // Add (update) it with a new item
        var listItem = new Reference<CoflnetUser>(new EntityId(7,3));
        list.Users.Add(listItem);

        // Should be added remotely now
        Assert.AreEqual(1,list.Users.Count);
       
        var bytes= MessagePackSerializer.Typeless.Serialize(list);

         // log it to the console to check the content

        var reconstrated = MessagePackSerializer.Typeless.Deserialize(bytes) as ResWithList;

        Assert.AreEqual(listItem.EntityId,reconstrated.Users[0].EntityId);
    }


    /// <summary>
    /// Tests that the Remote list intern information are set correctly with Messagepack deseralization callback
    /// </summary>
        [Test]
    public void RemoteListSeralizationTest() {
        var owner = new EntityId(5,3);

        DummyCore.Init(owner);

        // create resource
        var list = new ResWithList(owner);
        // assing it an id
        list.AssignId(DummyCore.Instance.EntityManager);

        var bytes= MessagePackSerializer.Typeless.Serialize(list);

         // log it to the console to check the content

        var reconstrated = MessagePackSerializer.Typeless.Deserialize(bytes) as ResWithList;

        // The field is corret
       // Assert.AreEqual("User",reconstrated.Users.Suffix);
        // the parent is set correctly as well
       // Assert.AreEqual(list.Id,list.Users.parent.Id);
    }


    [Test]
    public void RemoteListRemove() {
        var owner = new EntityId(5,3);

        DummyCore.Init(owner);

        // create resource
        var list = new ResWithList(owner);
        // register it
        list.AssignId(DummyCore.Instance.EntityManager);

        var listItem = new Reference<CoflnetUser>(new EntityId(7,3));

        // for testing add it directly
        list.Users.Add(listItem);

        // Add (update) it with a new item
        list.Users.Remove(listItem);

        // Should be removed remotely now
        Assert.AreEqual(0,list.Users.Count);
    }




    [Test]
    public void RemoteListResource() {
        var owner = new EntityId(5,3);


        

       // var list = new ListEntity<SourceReference>(owner);
        var stringList = new ListEntity<string>(owner);


        //var proxyData = GenerateAdd<SourceReference>(new SourceReference(5,54),owner,list);

        // add command
       // list.ExecuteCommand(proxyData);


        
       // proxyData = GenerateAdd<string>("app.id.new",owner,stringList);

       // stringList.ExecuteCommand(proxyData);



       // var bytes = MessagePackSerializer.Serialize(list);

       // Logger.Log(MessagePackSerializer.ToJson(bytes));
    }

    public class ResWithList : Entity, IMessagePackSerializationCallbackReceiver
    {
        private static CommandController Commands = new CommandController();

        public RemoteList<Reference<CoflnetUser>> Users;

        public ResWithList()
        {
            Users = new RemoteList<Reference<CoflnetUser>>(nameof(Users),this);
        }


        static ResWithList()
        {
            // add adapter for UserList commands
            RemoteList<Reference<CoflnetUser>>.AddCommands
                    (Commands,nameof(Users),m=>m.GetTargetAs<ResWithList>().Users);
        }

        public ResWithList(EntityId owner) : base(owner)
        {
            Users = new RemoteList<Reference<CoflnetUser>>(nameof(Users),this);
        }


        public override CommandController GetCommandController()
        {
            return Commands;
        }

        public void OnBeforeSerialize()
        {
            return;
        }

        public void OnAfterDeserialize()
        {
            Users.SetDetails(nameof(Users),this);
        }
    }
}
