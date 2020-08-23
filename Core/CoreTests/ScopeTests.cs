using NUnit.Framework;
using System.Collections;
using Coflnet.Core;
using Coflnet;
using System;
using System.Collections.Generic;
using MessagePack;

public class ScopeTests {

    [Test]
    public void ScopeCopyFieldsTest() {
        // Use the Assert class to test conditions.
        var resText = "Protected text";
        var Id = new EntityId(5,3);

        var res = new ScopeRes();
        res.Text.Value = resText;
        res.Id = Id;


        var outRes = new ScopeRes();

        res.AvailableScopes["textRead"].SetFields(outRes,res);
        // Text has been copied
        Assert.AreEqual(Id, outRes.Id);

        // Text has been copied
        Assert.AreEqual(resText, outRes.Text.Value);


        // the old text is still there
        Assert.AreEqual(resText, res.Text.Value);
    }

    [Test]
    public void ScopeCommandsTest() {
        // Use the Assert class to test conditions.
        var resText = "Protected text";
        var Id = new EntityId(5,3);

        var res = new ScopeRes();
        res.Text.Value = resText;
        res.Id = Id;

        var grantedSlugs = new string[]{"textRead","textWrite"};

        Assert.IsTrue(res.AvailableScopes.IsAllowedToExecute("getText",grantedSlugs));
        Assert.IsTrue(res.AvailableScopes.IsAllowedToExecute("setText",grantedSlugs));
        Assert.IsTrue(res.AvailableScopes.IsAllowedToExecute("getText",grantedSlugs));
        
    }
    [Test]
    public void ScopeCommandsUnauthorizedTest() {
        // Use the Assert class to test conditions.
        var resText = "Protected text";
        var Id = new EntityId(5,3);

        var res = new ScopeRes();
        res.Text.Value = resText;
        res.Id = Id;

        var grantedSlugs = new string[]{"text"};

        Assert.IsFalse(res.AvailableScopes.IsAllowedToExecute("getText",grantedSlugs));
        grantedSlugs = new string[]{"textRead"};
        Assert.IsFalse(res.AvailableScopes.IsAllowedToExecute("setText",grantedSlugs));
        
    }




    public class ScopeRes : Entity, IHasScopes,IMessagePackSerializationCallbackReceiver
    {
        private static ScopesList scopes;
        private static CommandController commands;

        public RemoteString Text;

        static ScopeRes()
        {
            scopes = new ScopesList();
            commands = new CommandController();

            RemoteString.AddCommands(
                commands,
                nameof(Text),
                d=>d.GetAs<ScopeRes>().Text,
                (data,s) 
                => data.GetAs<ScopeRes>().Text.Value = s);

            scopes.Add<TextReadScope>("textRead");
            scopes.Add<TextWriteScope>();
        }

        public ScopeRes()
        {
            Text = new RemoteString(nameof(Text),this);
        }

        public ScopesList AvailableScopes => scopes;

        public override CommandController GetCommandController()
        {
            return commands;
        }


        public void OnAfterDeserialize()
        {
            Text.SetDetails(nameof(Text),this);
        }

        public void OnBeforeSerialize()
        {
        }
    }

    public class TextReadScope : Scope
    {
        public TextReadScope()
        {
            Commands.Add("getText");
        }


        public override void SetFields(IHasScopes newRes, in IHasScopes oldRes)
        {
            Set<ScopeRes>(newRes,oldRes,(n, o)=>{
                n.Id = o.Id;
                n.Text =o.Text;
            });
        }

    }


    public class TextWriteScope : Scope,IHasSlug
    {

        public TextWriteScope()
        {
            Commands.Add("setText");
        }

        public override string Slug => "textWrite";
    }
}
