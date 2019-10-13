using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Coflnet;

public class ReferenceManagerTests {

    class TestResource : Referenceable
    {
        public override CommandController GetCommandController()
        {
            return globalCommands;
        }

        public override bool Equals(object obj)
        {         
            var casted = obj as TestResource;

            if (casted == null)
            {
                return false;
            }
            
            return Id == casted.Id;
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    [Test]
    public void ReferenceManagerTestsSimplePasses() {
        // Use the Assert class to test conditions.
        new ReferenceManager();
    }

    [Test]
    public void AddIdAndRedirect() {
        // Use the Assert class to test conditions.
        var manager = new ReferenceManager();
        var res = new TestResource();
        res.AssignId(manager);
        var newId = new SourceReference(1,12354);

        manager.UpdateIdAndAddRedirect(res.Id,newId);

        Assert.AreEqual(res,manager.GetResource(res.Id));
        Debug.Log("old found");
        Assert.AreEqual(res,manager.GetResource(newId));
    }

        [Test]
    public void AddIdAndRedirectSaveAndLoad() {
        // Use the Assert class to test conditions.
        var manager = new ReferenceManager();
        var res = new TestResource();
        res.AssignId(manager);
        var oldId = res.Id;
        var newId = new SourceReference(1,12354);

        manager.UpdateIdAndAddRedirect(res.Id,newId);

        manager.Save(newId,true);
        manager.Save(oldId,true);

        Assert.AreEqual(res,manager.GetResource(res.Id));
        Debug.Log("old found");
        Assert.AreEqual(res,manager.GetResource(newId));
    }
}
