using UnityEngine;

public class DontDestroyLoad : MonoBehaviour
{
	void Start()
	{
		DontDestroyOnLoad(gameObject);
	}
}
