using NUnit.Framework;
using System.Collections;
using Coflnet;
using MessagePack;
using System.Linq;
using static Coflnet.CommandData;

public class SignatureTest {

    [Test]
    public void SignatureTestSimplePasses() {
        // Use the Assert class to test conditions.

        var data = DummyData;

        var keyPair = EncryptionController.Instance.SigningAlgorythm.GenerateKeyPair();

        data.Sign(keyPair);

        // validate that signature exists
        Assert.IsNotNull(data.Signature);

        // validate signature
        
        Assert.IsTrue(data.ValidateSignature(keyPair.publicKey));
    }

    [Test]
    public void SignatureSerialization() {
        // Use the Assert class to test conditions.

        var data = DummyData;

        var keyPair = EncryptionController.Instance.SigningAlgorythm.GenerateKeyPair();

        data.Sign(keyPair);

        // "network" 
        var bytes = MessagePackSerializer.Serialize(data);

        var newData = MessagePackSerializer.Deserialize<CommandData>(bytes);

        // validate that signature exists
        Assert.IsNotNull(newData.Signature);

        // validate signature
        Assert.IsTrue(data.ValidateSignature(keyPair.publicKey));
    }

    [Test]
    public void SignatureSerializationInvalid() {
        // Use the Assert class to test conditions.

        var data = DummyData;

        var keyPair = EncryptionController.Instance.SigningAlgorythm.GenerateKeyPair();

        data.Sign(keyPair);

        // reverse the array to invalidate signature
        data.Signature.content = data.Signature.content.Reverse().ToArray();

        // "network" 
        var bytes = MessagePackSerializer.Serialize(data);
        var newData = MessagePackSerializer.Deserialize<CommandData>(bytes);

        // validate that signature exists
        Assert.IsNotNull(newData.Signature);

        
        // the signature is invalid
        Assert.IsFalse(newData.ValidateSignature(keyPair.publicKey));
    }


    [Test]
    public void SignatureUnset() {
        // Use the Assert class to test conditions.

        var data = DummyData;

        var keyPair = EncryptionController.Instance.SigningAlgorythm.GenerateKeyPair();


        // the signature wasn't set, should throw an exception
        Assert.Throws<SignatureInvalidException>(()=>{
            Assert.IsTrue(data.ValidateSignature(keyPair.publicKey));
        });
    }


    [Test]
    public void KeyPairManagerCreate()
    {
        var pair = KeyPairManager.Instance.GetOrCreateSigningPair(new EntityId(5,6));

        // pair was created
        Assert.NotNull(pair);
        Assert.NotNull(pair.publicKey);
        Assert.NotNull(pair.secretKey);

        //var data = DummyData;

        //data.Sign(pair);

        // pair signature is valid
        //Assert.IsTrue(data.ValidateSignature(pair.publicKey));
    }

    private CommandData DummyData=> new CommandData(new EntityId(1,2),0,"hi","test");

}
