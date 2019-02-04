using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coflnet;
using Coflnet.Server;
using MessagePack;
using System.Collections.Concurrent;

public class CoflnetMain : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
		Debug.Log("Starting test");

		var id = ReferenceManager.Instance.CreateReference(new CoflnetUser()
		{
			Age = 6
		});

		var ohneU = ReferenceManager.Instance.References[id].Resource;
		var u = (object)new CoflnetUser() { Age = 7 };
		Debug.Log(((CoflnetUser)MessagePackSerializer.Typeless.Deserialize(MessagePackSerializer.Typeless.Serialize(ohneU))).Age);
		Debug.Log(MessagePackSerializer.ToJson(MessagePackSerializer.Serialize((u), MessagePack.Resolvers.TypelessContractlessStandardResolverAllowPrivate.Instance)));


		Debug.Log("Alter ohne s ist: " + ((CoflnetUser)ohneU).Age);
		var serialized = MessagePackSerializer.Typeless.Serialize(ReferenceManager.Instance.References);
		Debug.Log("Id ist: " + id.ToString());
		Debug.Log("serialized ist:" + MessagePackSerializer.ToJson(serialized));

		var dict = ((ConcurrentDictionary<SourceReference, Reference<Referenceable>>)MessagePackSerializer.Typeless.Deserialize(serialized));
		Reference<Referenceable> user;
		dict.TryGetValue(id, out user);
		Debug.Log(user.ReferenceId.ToString());
		Debug.Log("Alter ist: " + ((CoflnetUser)user.Resource).Age);
	}


}
