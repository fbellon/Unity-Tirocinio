using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;



public class RobotLauncher : MonoBehaviour
{
	public Rigidbody ball;
	public RobotSim robot;
	public Vector3 initialPose;
	public BasicLaunchSim launcher;
	public float initialSpin;
	public float initialSlice;
	public Vector3 initialVelocity;
	public bool launch_event;
	public bool clear_event;

  
	void Start ()
	{
		initialPose.x = ball.position.x;
		initialPose.y = ball.position.y;
		initialPose.z = ball.position.z;
		launch_event = false; 
    
		initialVelocity.x = 0;
		initialVelocity.y = 4;
		initialVelocity.z = 25;

		initialSpin = 0;

		enabled = true;
	}


	void FixedUpdate ()
	{
		if (launch_event)
			launch_event = false;
		if (clear_event)
			clear_event = false;
	}
}
