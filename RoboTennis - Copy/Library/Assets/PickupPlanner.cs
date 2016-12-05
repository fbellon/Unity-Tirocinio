using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class PickupPlanner : MonoBehaviour
{
    public BasicLaunchSim launchSim;
    public BasicMotionPlanner planner;
    public RobotSim robot;

    public bool operate, pickup, crossing;
    public int STATE_P;

    Vector2 startPosition;
    Vector2[] crossingPath;

    Vector3 ballPos;

    int nearest, crossing_index;
    float distance, mindistance, target_theta, travelDistance;

    void Start()
    {
        // Variabili locali
        pickup = false;
        operate = false;
        nearest = -1;
        distance = 100;
        STATE_P = 0;
        ballPos.Set(0, 0, 0);
        crossing = false;
        target_theta = 0;
        crossingPath = new Vector2[2];
        crossing_index = 0;

        crossingPath[0].Set(8.0f, -3);
        crossingPath[1].Set(8.0f, 3);
    }



    void updateState()
    {
        switch (STATE_P)
        {
            case 0:
                planner.reverse_movement_enabled = false;
                STATE_P = 1;
                break;



            case 1:
            //    Debug.Log("STATO 1");


                if (launchSim.ball_list.Count > 0)
                {
                    if (Mathf.Sign(robot.transform.position.z) == Mathf.Sign(ballPos.z))
                        crossing = false;
                    else
                        crossing = true;

                    if (robot.transform.position.z < 0)
                        crossing_index = 0;
                    else
                        crossing_index = 1;

                    if (crossing)
                    {
                        planner.goal.Set(crossingPath[crossing_index][0], crossingPath[crossing_index][1], target_theta, mindistance / 2.0f);
                        crossing_index = (crossing_index + 1) % 2;
                        planner.target_set = true;
                        STATE_P = 3;
                    }
                    else
                    {
                        if (ballPos.z >= 17.8f)
                            ballPos.z = 17.8f;
                        if (ballPos.z <= -17.8f)
                            ballPos.z = -17.8f;
                        if (ballPos.x >= 8.8f)
                            ballPos.x = 8.8f;
                        if (ballPos.x <= -8.8f)
                            ballPos.x = -8.8f;
                        planner.goal.Set(ballPos.x, ballPos.z, target_theta, mindistance / 2.0f);
                        planner.target_set = true;
                        STATE_P = 2;
                    }
                }
                else
                {
                    STATE_P = 0;
                    pickup = false;
                    operate = false;
                    planner.reverse_movement_enabled = true;
                }
                break;

            case 2:
            //    Debug.Log("STATO 2");

        //        Debug.Log(launchSim.ball_list.Count);
                if (launchSim.ball_list.Count > 0)
                {
          //          Debug.Log("Switch to stato 1");
                    // Abbiamo altre palline
                    STATE_P = 1;
                }
                else
                {
                    if (Mathf.Sign(robot.transform.position.z) == Mathf.Sign(-8))
                        crossing = false;
                    else
                        crossing = true;

                    if (robot.transform.position.z < 0)
                        crossing_index = 0;
                    else
                        crossing_index = 1;

                    if (crossing)
                    {
                        distance = Mathf.Sqrt(Mathf.Pow(crossingPath[crossing_index].x - robot.transform.position.x, 2) + Mathf.Pow(crossingPath[crossing_index].y - robot.transform.position.z, 2));
                        planner.goal.Set(crossingPath[crossing_index][0], crossingPath[crossing_index][1], target_theta, mindistance / 2.0f);
                        crossing_index = (crossing_index + 1) % 2;
                        planner.target_set = true;
                        STATE_P = 3;
                    }
                    else
                    {
                        planner.goal.Set(0, -8, 0, 10);
                        planner.target_set = true;
                        STATE_P = 4;
                    }
                }
                break;


            case 3:
                if (launchSim.ball_list.Count > 0)
                {
                    if (crossing)
                    {
                        distance = Mathf.Sqrt(Mathf.Pow(crossingPath[crossing_index].x - robot.transform.position.x, 2) + Mathf.Pow(crossingPath[crossing_index].y - robot.transform.position.z, 2));
                        planner.goal.Set(crossingPath[crossing_index][0], crossingPath[crossing_index][1], target_theta, distance / 2.0f);
                        planner.target_set = true;
                        crossing = false;
                        STATE_P = 3;
                    }
                    else
                    {
                        if (ballPos.z >= 17.8f)
                            ballPos.z = 17.8f;
                        if (ballPos.z <= -17.8f)
                            ballPos.z = -17.8f;
                        if (ballPos.x >= 8.8f)
                            ballPos.x = 8.8f;
                        if (ballPos.x <= -8.8f)
                            ballPos.x = -8.8f;
                        planner.goal.Set(ballPos.x, ballPos.z, target_theta, mindistance / 2.0f);
                        planner.target_set = true;
                        STATE_P = 2;
                    }
                }
                else
                {
                    if (crossing)
                    {
                        operate = false;
                        pickup = false;
                        planner.reverse_movement_enabled = true;
                        STATE_P = 0;
                        crossing = false;
                        STATE_P = 3;
                    }
                    else
                    {
                        planner.goal.Set(0, -8, 0, 10);
                        planner.target_set = true;
                        STATE_P = 4;
                    }
                }

                break;

            case 4:
                operate = false;
                pickup = false;
                planner.reverse_movement_enabled = true;
                STATE_P = 0;
                break;


            default:
                break;
        }
    }


    void FixedUpdate()
    {

        if (operate)
        {

            switch (STATE_P)
            {
                case 0:
                    // Stato idle. Andiamo allo stato GO con parametri: centro
                    if (pickup)
                        updateState();
                    break;


                case 1:
                    // Scegli la pallina più vicina (tra quelle ferme!!! qualcuna potrebbe essere ancora in volo vicino al robot) e settala come obbiettivo per il planner
                    mindistance = 10;
                    distance = 100;
                    nearest = 0;
                    if (launchSim.ball_list.Count > 0)
                    {
                     //   Debug.Log("Mancano " + launchSim.ball_list.Count + " palle");
                        for (int i = 0; i < launchSim.ball_list.Count; ++i)
                        {
                            if (launchSim.stopped[i])
                            {
                                ballPos = launchSim.ball_list.ElementAt(i).GetComponent<Rigidbody>().transform.position;
                                distance = Mathf.Sqrt(Mathf.Pow(ballPos.x - robot.transform.position.x, 2) + Mathf.Pow(ballPos.z - robot.transform.position.z, 2));

                                if (distance < mindistance)
                                {
                                    mindistance = distance;
                                    nearest = i;
                                }
                            }
                        }

                        // Vai alla pallina di indice i che a questo punto è la più vicina (per target theta NON usare robot.transform.position)

                        ballPos = launchSim.ball_list.ElementAt(nearest).GetComponent<Rigidbody>().transform.position;
                        distance = Mathf.Sqrt(Mathf.Pow(ballPos.x - robot.transform.position.x, 2) + Mathf.Pow(ballPos.z - robot.transform.position.z, 2));
                        target_theta = Mathf.Rad2Deg * Mathf.Atan2(ballPos.x - robot.position.x, ballPos.z - robot.position.z);
              //          Debug.Log(ballPos);

                        updateState();
                    }
                    else
                    {
                        // Finito le palline
                        Debug.Log("Finita la raccolta palline");
                        pickup = false;
                        updateState();
                    }
                    break;


                case 2:
                    // in moto; non far nulla
                    if (planner.arrived && planner.aligned && !planner.target_set)
                        updateState();
                    // Se siamo vicini a una qualche pallina, distruggiamo la sua istanza
                    for (int i = 0; i < launchSim.ball_list.Count; ++i)
                    {
                        ballPos = launchSim.ball_list.ElementAt(i).GetComponent<Rigidbody>().transform.position;
                        distance = Mathf.Sqrt(Mathf.Pow(ballPos.x - robot.transform.position.x, 2) + Mathf.Pow(ballPos.z - robot.transform.position.z, 2));

                        if (distance < 0.6f && ballPos.y <= 0)
                        {
                            GameObject.Destroy(launchSim.ball_list.ElementAt(i), 0.5f);
                            launchSim.ball_list.RemoveAt(i);
                            launchSim.ballData_list.RemoveAt(i);
                            launchSim.stopped.RemoveAt(i);
                            launchSim.ballBounce_list.RemoveAt(i);
                        }
                    }
                    break;


                case 3:
                    // Crossing; in moto; non far nulla;
                    if (planner.arrived && planner.aligned && !planner.target_set)
                        updateState();
                    break;

                case 4:
                    // Ritorno; non far nulla
                    if (planner.arrived && planner.aligned && !planner.target_set)
                        updateState();
                    break;

                default:
                    break;

            }
        }
    }

}
