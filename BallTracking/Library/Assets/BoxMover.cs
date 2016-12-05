using UnityEngine;
using System.Collections;

public class BoxMover : MonoBehaviour {
    Rigidbody box;

    public Vector3 pos_robot;
    public Vector3 vel_robot;
    public Vector3 acc_robot;
    public float k1, kattr;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        pos_robot += vel_robot * k1;
        vel_robot += acc_robot * k1;
        acc_robot -= vel_robot * kattr;
	}
}
