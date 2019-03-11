using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Coflnet.Unity;

public class InstancePasser : MonoBehaviour
{
	public string identifier;
	public GameObject[] objects;
	public bool enableAll = true;

	public InstancePasser(string identifier)
	{
		this.identifier = identifier;
	}

	public void ChangeSetting(bool decission)
	{
		Debug.Log("Changed " + identifier + " to " + decission);
		PrivacyController.instance.SetDecission(identifier, decission);
		MenuController.instance.ChangeObjectState(objects, decission);
		if (!enableAll && decission)
			return;
		for (int i = 0; i < objects.Length; i++)
		{
			objects[i].GetComponent<Toggle>().isOn = decission;
			InstancePasser ip = objects[i].GetComponent<InstancePasser>();
			ip.ChangeSetting(decission);
		}
	}

}
