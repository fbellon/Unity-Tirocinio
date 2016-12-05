using UnityEngine;
using System;

public class WheelControl : MonoBehaviour
{
  public float[] target_velocity;
  public float[] current_velocity;
  public float kp, ki;

  float[] perror, ierror;

  float dt;

  RobotSim robot;


  void Start()
  {
    target_velocity = new float[2];
    current_velocity = new float[2];
    
    perror = new float[2];
    ierror = new float[2];
    dt = 0.2f;

    robot = this.GetComponent<RobotSim>();
    
    kp = 10f;
    ki = 0.25f;
  }

  void FixedUpdate()
  {
    // Calcola l'avanzamento per ogni ruota, sistema di rif. locale (versore Z locale)

    // Left
    current_velocity[0] = transform.InverseTransformDirection(robot.components[1].velocity).z;
    
    perror[0] = target_velocity[0] - current_velocity[0];
    ierror[0] = ierror[0] + (perror[0] * dt);

    robot.updated_motor_torque[0] = kp * perror[0] + ki * ierror[0];
  
    // Right
    current_velocity[1] = transform.InverseTransformDirection(robot.components[2].velocity).z;

    perror[1] = target_velocity[1] - current_velocity[1];
    ierror[1] = ierror[1] + (perror[1] * dt);

    robot.updated_motor_torque[0] = kp * perror[0] + ki * ierror[0];
    robot.updated_motor_torque[1] = kp * perror[1] + ki * ierror[1];

  }
}
