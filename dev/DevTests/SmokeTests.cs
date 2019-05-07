using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Coflnet.Dev;


public class SmokeTests {

    [Test]
    public void SmokeTestsSimplePasses() {
        DevCore.Init(new Coflnet.SourceReference(1,1,1,123456));
    }

    [Test]
    public void ClientServerSimulation() {
        DevCore.Init(new Coflnet.SourceReference(1,1,1,123456));

       // DevCore.Instance.SendCommand()
    }
}
