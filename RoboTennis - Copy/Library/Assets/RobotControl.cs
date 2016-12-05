using UnityEngine;
using System;

public class RobotControl : MonoBehaviour
{
  // Time
  float time_now;
  float time_current;

  // Controller 
  public float linear_velocity_setpoint;
  public float angular_velocity_setpoint;

  // Coefficienti controllori
  public float linear_kp, angular_kp;

  // Output
  public float linear_output;
  public float angular_output;

  // Robot status
  RobotSim robot;
  WheelControl wheel_controller;

  public float linear_velocity_current;
  public float angular_velocity_current;

  // Errori
  float linear_perror, linear_perror_old;
  float angular_perror, angular_perror_old;
  

  // Nota: valori tarati per 0.003333 s
  void Start()
  {
    robot = this.GetComponent<RobotSim>();
    wheel_controller = this.GetComponent<WheelControl>();

    linear_kp = 10.0f;
    angular_kp = 20.0f;

  }


  void FixedUpdate()
  {
    // Aggiornamento grandezze 
    linear_velocity_current = robot.linearVelocity[2];    // Z
    angular_velocity_current = robot.angularVelocity[1];  // Y

    // Aggiornamento errori
    linear_perror = linear_velocity_setpoint - linear_velocity_current;
    angular_perror = angular_velocity_setpoint - angular_velocity_current;

    // Calcolo output (solo Prop. per ora)
    linear_output = linear_kp * linear_perror;
    angular_output = angular_kp * angular_perror;
    
    // Sogliatura max vel. rotazione
    if (angular_output > 5)
    {
      angular_output = 5;
    }
    if (angular_output < -5)
    {
      angular_output = -5;
    }

    // Applica output alle ruote
    wheel_controller.target_velocity[0] = wheel_controller.current_velocity[0] + linear_output + angular_output;
    wheel_controller.target_velocity[1] = wheel_controller.current_velocity[1] + linear_output - angular_output;

  }
}