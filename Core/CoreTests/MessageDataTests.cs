using System.Collections;
using System.Collections.Generic;
using Coflnet;
using NUnit.Framework;

public class MessageDataTests  {

	[Test]
	public void SignatureTest()
	{
		var msg = new MessageData(){headers = new MessageDataHeader()};
		msg.headers.Add(1,new byte[]{5,3});

	}
}
