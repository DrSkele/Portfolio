using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

    float BulletSpeed = 40f;
    float LifeTime = 3f;

	// Use this for initialization
	void Start () {
        Destroy(gameObject, LifeTime);
    }
	
	// Update is called once per frame
	void Update () {
        transform.Translate(Vector3.forward * BulletSpeed * Time.deltaTime);
    }
}
