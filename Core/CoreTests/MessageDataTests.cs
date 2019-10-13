using System.Collections;
using System.Collections.Generic;
using Coflnet;
using NUnit.Framework;
using UnityEngine;

public class MessageDataTests  {

	[Test]
	public void SignatureTest()
	{
		var msg = new MessageData(){headers = new MessageDataHeader()};
		Debug.Log(msg.SignableContent.Length);
		msg.headers.Add(1,new byte[]{5,3});
		Debug.Log(msg.SignableContent.Length);

	}
}
