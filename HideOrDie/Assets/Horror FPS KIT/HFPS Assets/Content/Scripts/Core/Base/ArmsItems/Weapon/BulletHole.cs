using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class BulletHole : MonoBehaviour {

	public List<Material> BulletHoles = new List<Material>();

	void Start () {
		MeshRenderer renderer = GetComponent<MeshRenderer>();
		renderer.material = BulletHoles[Random.Range(0, BulletHoles.Count)];
	}
}