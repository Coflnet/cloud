using NUnit.Framework;
using Coflnet.Dev;


public class SmokeTests {

    [Test]
    public void SmokeTestsSimplePasses() {
        DevCore.Init(new Coflnet.EntityId(1,1,1,123456));
    }

    [Test]
    public void ClientServerSimulation() {
        DevCore.Init(new Coflnet.EntityId(1,1,1,123456));

       // DevCore.Instance.SendCommand()
    }
}
