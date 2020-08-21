using System.Collections;
using System.Collections.Generic;
using Coflnet;
using NUnit.Framework;

public class CommandDataTests  {

	[Test]
	public void SignatureTest()
	{
		var msg = new CommandData(){Headers = new CommandDataHeader()};
		msg.Headers.Add(1,new byte[]{5,3});

	}
}
