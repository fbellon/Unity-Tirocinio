using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class BasicLaunchSim : MonoBehaviour
{
  public Rigidbody ballSim;
  GameObject ballClone;

  public DemoSetup demo;
  public RobotLauncher launcher;
  public PhysicsStep physics;
  public bool operate;
  public float spinRPM, sliceRPM;
	public bool generazione = false;
  
  float dt, shotDistance;
  public Vector3 position, velocity;
  
  Vector3[] resultM;
  Vector3 Vg;
  public float Vmagnitude;

  public List< GameObject > ball_list;
  public List< Vector3[] > ballData_list;
  public List< bool > stopped;
  public List<int> ballBounce_list;
  Vector3[] ballData;
  Vector3 Planding;
  float target_theta;


  void Start()
  {
    physics = new PhysicsStep();
    operate = false;
    dt = Time.fixedDeltaTime;
    resultM = new Vector3[3];
    ballData = new Vector3[3];
    shotDistance = 0;

    ball_list = new List<GameObject>();
    ballData_list = new List<Vector3[]>();
    ballBounce_list = new List<int>();
    stopped = new List< bool >();
    ball_list.Clear();
    ballData_list.Clear();
    stopped.Clear();

  }
  
  
  void FixedUpdate()
  {
    // Intercetta l'evento di lancio
    if(launcher.launch_event)
    {
			generazione = true;
      //  launcher.launch_event = false;
        Debug.Log("Launch basic sim");
      launcher.clear_event = true;
      position = launcher.initialPose;
      velocity = launcher.initialVelocity; 
      spinRPM = launcher.initialSpin;
      sliceRPM = launcher.initialSlice;
      operate = true;

      ballData[0] = position;
      ballData[1] = velocity;
      ballData[2].x = spinRPM;
      ballData[2].y = sliceRPM;
      
      ballClone = Instantiate(ballSim.gameObject) as GameObject;
      //Debug.Log("LANCIO");
      
      ball_list.Add((GameObject)ballClone);
      ballData_list.Add((Vector3[])ballData.Clone());
      stopped.Add(false);
      ballBounce_list.Add((int)0);

      return;
    }


    // Intercetta l'evento di "pulizia"
    if (launcher.clear_event)
    {
      ball_list.Clear();
      ballData_list.Clear();
      stopped.Clear();

      return;
    }

    
    if(operate)
    {
      // Aggiorna la fisica per ognuna delle palline lanciate, solo se sono in movimento 
      for (int i = 0; i < ball_list.Count; ++i )
      {
        if (stopped[i] == false)
        {
          position = ballData_list[i][0];
          velocity = ballData_list[i][1];
          spinRPM = ballData_list[i][2].x;
          sliceRPM = ballData_list[i][2].y;

          resultM = physics.Step(position, velocity, spinRPM, sliceRPM, dt);

          ballData_list[i][0] = resultM[0];
          ballData_list[i][1] = resultM[1];
          ballData_list[i][2].x = resultM[2].x;
          ballData_list[i][2].y = resultM[2].y;

          // Aggiorna il contatore dei rimbalzi, solo la prima volta
          if (ballData_list[i][0].y <= 0)
            if(ballBounce_list[i] == 0)
              ballBounce_list[i] = 1;

          // Genera il lancio di ritorno se ci sono le condizioni
          if (  ballData_list[i][0].y > 0 && ballData_list[i][1].y < 0.3f && ballBounce_list[i] == 1)
          {
              //Debug.Log("Lancio di ritorno");
            Planding.x = (UnityEngine.Random.value * 8)-4;
            Planding.y = 0;
            Planding.z = -((UnityEngine.Random.value * 4) + 8);
            
            shotDistance = Mathf.Sqrt(Mathf.Pow(Planding.x - ballData_list[i][0].x, 2) + Mathf.Pow(Planding.z - ballData_list[i][0].z, 2));
            target_theta = Mathf.Rad2Deg * Mathf.Atan2(Planding.x - ballData_list[i][0].x, Planding.z - ballData_list[i][0].z);
            
            // Se il tiro è valido, allora racchettiamo
            demo.hitRec = demo.sSpace.GetHit(shotDistance, 0.4f, 1+(UnityEngine.Random.value), 0, 0);
            if (demo.hitRec.hitCounter)
            {
              ballData_list[i][1].x = ((float)(demo.hitRec.v0x / 100.0f)) * Mathf.Sin(Mathf.Deg2Rad * target_theta);
              ballData_list[i][1].y = ((float)(demo.hitRec.v0y / 100.0f));
              ballData_list[i][1].z = ((float)(demo.hitRec.v0x / 100.0f)) * Mathf.Cos(Mathf.Deg2Rad * target_theta);
              ballData_list[i][2][0] = 0;
              ballData_list[i][2][1] = 0;
            }
            ballBounce_list[i] = 2;
          }

          // Check per disabilitare la fisica su palle ferme
          if (ballData_list[i][0].y <= 0 && ballData_list[i][1].magnitude < 0.02f)
          {
            stopped[i] = true;
            ballData_list[i][1].Set(0, 0, 0);
          }
        }
       
        // Aggiorna la posizione
        ball_list[i].GetComponent<Rigidbody>().transform.position = ballData_list[i][0];
      }

    }

  }
}