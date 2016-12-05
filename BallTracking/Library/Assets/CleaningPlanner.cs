using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class CleaningPlanner : MonoBehaviour
{
  public RobotSim robot;
  public RobotControl base_controller;
  public BasicMotionPlanner planner;
  public Rigidbody net;
    
  public bool operate, aligned, clean_field, clean_lines;
  Vector4 startingPose;

  float velocity_target;
  float omega_target;
  float orientation_target, orientation_current;
  Vector3 position_current, position_goal;
  float next_trigger;
  float gDist;

  // Vertici delle linee di campo
  Vector4[] goals;

  float error1, error2, angle_error;  
  float orientation_epsilon, omega_setpoint, kp;
 
  Vector2 direction;
  Vector3 net_p;
  
  int index, goalCount;

  public int STATE_C;



  void Start()
  {
    operate = false;
    clean_field = false;
    clean_lines = false;

    STATE_C = 0;

    next_trigger = 6;
    startingPose.Set(-5, -12, 90, next_trigger);

    orientation_epsilon = 2.5f;

    kp = 0.05f;

    direction.Set(90, 270);

    net_p.Set(0, -2, 0);
    net.transform.position = net_p;


    goals = new Vector4[14];
    goalCount = 0;

    goals[0].Set( 5.4f,   -2,       180,  4);
    goals[1].Set( 5.4f,   -11.89f,  270,  6);
    goals[2].Set( -5.4f,  -11.89f,  0,    7);
    goals[3].Set(-5.4f,   -2,       90, 6);
    goals[4].Set( -4,     -2,       180,  2);
    goals[5].Set( -4,     -11.89f,  0,    6);
    goals[6].Set( -4,     -6.4f,    90,   3);
    goals[7].Set( 0,      -6.4f,    0,    3);
    goals[8].Set( 0,      -2,       180,  3);
    goals[9].Set( 0,      -6.4f,    90,   3);
    goals[10].Set(4,      -6.4f,    180,  3);
    goals[11].Set(4,      -11.89f,  0,    3);
    goals[12].Set(4,      -2,       180,  6);
    goals[13].Set(-5,     -11.89f,  90,   8); 

  }



  public void Reset()
  {
    base_controller.linear_velocity_setpoint = 0;
    base_controller.angular_velocity_setpoint = 0;
    operate = false;
    clean_field = false;
    clean_lines = false;
    STATE_C = 0;
    index = 0;
    orientation_target = direction[index];
    net_p.Set(0, -2, 0);
    net.transform.position = net_p;
    
  }



  void updateState()
  {
    switch (STATE_C)
    {

      // Idle
      case 0:
        if (clean_lines)
          STATE_C = 4;
        else
          STATE_C = 1;

        planner.operate = true;
        planner.reverse_movement_enabled = false;
        planner.aligned = false;
        planner.arrived = false;
        planner.target_set = true;
        break;


      // Vai al punto di partenza per spazzare
      case 1:
        planner.aligned = false;
        planner.arrived = false;
        orientation_target = 90;
        net.transform.position.Set(0, 0.08f, 0);
        STATE_C = 2;
        break;


      case 2:
        if (Mathf.Abs(position_current.z) < 2f )
        {
          // Procediamo con la parte di pulizia delle righe
          base_controller.linear_velocity_setpoint = 0;
          base_controller.angular_velocity_setpoint = 0;
          planner.operate = true;
          planner.goal = goals[goalCount];
          planner.target_set = true;
          clean_field = false;
          clean_lines = true;
          STATE_C = 4;
        }
        else
        {
          // Dobbiamo ancora spazzare tutto, giriamo a serpentina
          index++;
          orientation_target = direction[index % 2];
          next_trigger *= -1;
          STATE_C = 3;
        }
        break;

      // Turn
      case 3:
        STATE_C = 2;
        break;

      case 4:
        if( clean_lines )
        {
          planner.goal = goals[goalCount];
          planner.aligned = false;
          planner.arrived = false;
          planner.target_set = true;
        }
        else
        {
          // Altrimenti se abbiamo finito torniamo in idle
          net_p.Set(0, -2, 0);
          net.transform.position = net_p;
          base_controller.linear_velocity_setpoint = 0;
          base_controller.angular_velocity_setpoint = 0;
          clean_field = false;
          clean_lines = false;
          operate = false;
          STATE_C = 0;
        }
        break;

      default:
        Reset();
        break;
    }
  }

  

  float Align()
  {
    error1 = orientation_target - orientation_current;
    error2 = (360 - Mathf.Abs(error1)) * Mathf.Sign(-error1);
    angle_error = Mathf.Min(Mathf.Abs(error1), Mathf.Abs(error2));
    if (angle_error == Mathf.Abs(error1))
      angle_error *= Mathf.Sign(error1);
    else
      angle_error *= Mathf.Sign(error2);

    if (Mathf.Abs(angle_error) < 0.25f)
    {
      if (!aligned)
        aligned = true;

      omega_setpoint = 0;
    }
    else
    {
      if (aligned)
        aligned = false;

      omega_setpoint = kp * angle_error;

      // Threshold
      if (Mathf.Abs(omega_target) > 1)
        omega_setpoint = 1 * Mathf.Sign(omega_setpoint);
    }
    return omega_setpoint;
  }




  void FixedUpdate()
  {
    if(operate)
    {
      // Vai avanti fino ai limiti del campo
      // Gira a sx o dx finché non ti allinei all'opposto
      // Rinse & Repeat
      position_current = robot.position;
      orientation_current = robot.orientation;
      //omega_current = robot.angularVelocity.y;


      switch(STATE_C)
      {
        // Idle
        case 0:
          if (clean_field)
          {
            clean_lines = false;
            planner.goal = startingPose;
            updateState();
          }

          if(clean_lines)
          {
            net_p.Set(0, -2, 0);
            clean_field = false;
            planner.goal = goals[goalCount];
            updateState();
          }

          break;

        // Aspettiamo che il planner normale abbia condotto il robot al punto di partenza
        case 1:
          if (planner.aligned && planner.arrived)
            updateState();
            
          break;

        // Diamo comandi in velocità alla base
        case 2:
          net_p.x = position_current.x - 1 * Mathf.Sin(Mathf.Deg2Rad * orientation_current);
          net_p.z = position_current.z - 1 * Mathf.Cos(Mathf.Deg2Rad * orientation_current);
          net_p.y = 0.05f;
          net.transform.position = net_p;
          net.transform.rotation = robot.transform.rotation;

          if (Mathf.Abs(next_trigger - position_current.x) < 0.1f)
            updateState();            
          else
          {
            velocity_target = 1.4f;
            base_controller.linear_velocity_setpoint = velocity_target;
            omega_target = Align();
            base_controller.angular_velocity_setpoint = omega_target;
          }
          break;

        // Turn
        case 3:
          net_p.x = position_current.x - 1 * Mathf.Sin(Mathf.Deg2Rad * orientation_current);
          net_p.z = position_current.z - 1 * Mathf.Cos(Mathf.Deg2Rad * orientation_current);
          net_p.y = 0.05f;
          net.transform.position = net_p;
          net.transform.rotation = robot.transform.rotation;

          if( Mathf.Abs(robot.position.z) >= 1.75f)
            base_controller.linear_velocity_setpoint = 1.25f;
          else
            base_controller.linear_velocity_setpoint = 0;
          
          if(orientation_target == 90)
            base_controller.angular_velocity_setpoint = 1.75f;
          else
            base_controller.angular_velocity_setpoint = -1.75f;
          
          if (Mathf.Abs(orientation_target - orientation_current) < orientation_epsilon)
          {
            base_controller.angular_velocity_setpoint = 0;
            updateState();
          }
          break;

        
        case 4:
          net.transform.position.Set(0, -2, 0);

          if (planner.aligned && planner.arrived && !planner.target_set)
          {
            goalCount++;
            
            if (goalCount < goals.Length)
              clean_lines = true;
            else
              clean_lines = false;

            updateState();
          }
          break;
          

        default:
          Reset();
          break;
      }
    }
    else
    {
      if (clean_field || clean_lines)
        Reset();
    }
    


  }
}