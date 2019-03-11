using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coflnet.Unity
{
	public class DummyGameObject : MonoBehaviour
	{

		public static GameObject Instance { get; }

		static DummyGameObject()
		{
			Instance = new GameObject();
		}
	}
}


