using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class BasicMotionPlanner : MonoBehaviour
{

    public float manual_velocity;

    public RobotSim robot;
    public RobotControl base_controller;
    public bool operate, aligned, arrived, target_set, reverse_movement_enabled;
    float orientation_setpoint, orientation_current;
    public float velocity_target, velocity_current, omega_target, omega_current;
    public Vector4 goal;
    Vector3 position_current;

    float error1, error2, angle, angle_direction;
    float kp, orientation_epsilon, pose_epsilon;
    float alignTime, currentTime, stayAlignedTime;

    public int STATE_M;
    public float turn_speed;

    float acc_percent, start_c, end_c;
    public float travel_t, cruise_t, travel_d, accel_t, cruise_v;

    float direction;



    void Start()
    {
        STATE_M = 0;
        operate = true;
        target_set = false;
        reverse_movement_enabled = true;

        goal.Set(5, -3, 0, 10);


        orientation_current = robot.orientation;
        orientation_setpoint = 0;
        orientation_epsilon = 1.0f;

        turn_speed = Mathf.PI * 0.75f;

        acc_percent = 0.4f;
        kp = 0.01f;
    }



    public void Reset()
    {
        STATE_M = 0;
        //   operate = false;
        target_set = false;
        orientation_current = robot.orientation;
        orientation_setpoint = robot.orientation;

        base_controller.linear_velocity_setpoint = 0;
        base_controller.angular_velocity_setpoint = 0;

        currentTime = 0;
        alignTime = 0;
        stayAlignedTime = 0;
    }



    float ShortestAngle()
    {
        // Calcola la distanza di allineamento da una parte
        error1 = orientation_setpoint - orientation_current;
        // Calcola la distanza dall'altra
        error2 = (360 - Mathf.Abs(error1)) * Mathf.Sign(-error1);

        // Ricava la direzione migliore
        angle = Mathf.Min(Mathf.Abs(error1), Mathf.Abs(error2));

        // La "direzione" corretta è data dal segno dell'errore
        if (angle == Mathf.Abs(error1))
            angle *= Mathf.Sign(error1);
        else
            angle *= Mathf.Sign(error2);

        return angle;
    }



    float ProgressiveTurn()
    {
        // Calcola direzione migliore di allineamento
        angle_direction = ShortestAngle();

        // Allina l'orientamento del robot alla direttrice
        if (Mathf.Abs(angle_direction) > orientation_epsilon)
        {
            // Se eravamo in fase di allineamento, e ci siamo disallineati (overshoot), aggiorniamo la flag
            if (aligned)
            {
                aligned = false;
                stayAlignedTime = 0;
            }
            currentTime += Time.fixedDeltaTime;
            omega_target = kp * angle_direction;
        }
        else
        {
            stayAlignedTime += Time.fixedDeltaTime;
            if (stayAlignedTime > 0.5f)
            {
                aligned = true;
                alignTime = currentTime;
                omega_target = 0;
                updateState();
            }
        }
        return omega_target;
    }



    float QuickTurn(float radsec)
    {
        // Calcola direzione migliore di allineamento
        angle_direction = ShortestAngle();

        // Allinea l'orientamento del robot alla direttrice
        if (Mathf.Abs(angle_direction) > orientation_epsilon && !aligned)
        {
            currentTime += Time.fixedDeltaTime;
            omega_target = radsec * angle_direction * Mathf.Abs(1.0f / angle_direction);
        }
        else
        {
            aligned = true;
            alignTime = currentTime;
            omega_target = 0;
        }

        if (omega_current < 0.001f && aligned)
            updateState();

        return omega_target;
    }



    void idle()
    {
        base_controller.linear_velocity_setpoint = 0;
        base_controller.angular_velocity_setpoint = 0;
    }



    // FSM: Gestione transizione tra stati
    void updateState()
    {

        switch (STATE_M)
        {
            case 0:
                // nel punto in cui ci troviamo target_set è sempre vero
                if (!arrived || !aligned)
                {
                    currentTime = 0;
                    alignTime = 0;
                    stayAlignedTime = 0;
                    STATE_M = 1;
                }
                else
                {
                    target_set = false;
                    idle();
                }
                break;

            case 1:
                if (arrived)
                {
                    currentTime = 0;
                    alignTime = 0;
                    stayAlignedTime = 0;
                    target_set = false;
                    idle();
                    STATE_M = 0;
                }
                else
                {
                    // Genera il profilo di velocità a trapezio avendo dist, tempo, e t_align
                    // Influenza inclinazione delle rampe generate e vel max
                    cruise_t = travel_t * (1 - acc_percent);
                    accel_t = travel_t * acc_percent / 2.0f;
                    cruise_v = travel_d / (cruise_t + accel_t);
                    currentTime = 0;
                    STATE_M = 2;
                }
                break;

            case 2:
                currentTime = alignTime = stayAlignedTime = 0;
                aligned = false;
                STATE_M = 3;
                break;

            case 3:
                currentTime = 0;
                alignTime = 0;
                stayAlignedTime = 0;
                target_set = false;
                idle();
                STATE_M = 0;
                break;

            case 4:
                break;

            default:
                Reset();
                operate = false;
                target_set = false;
                STATE_M = 0;
                break;
        }
    }



    void FixedUpdate()
    {
        if (Input.GetKey("k"))
        {
            STATE_M = 4;
            velocity_target = 0;
            omega_target = 0;
            arrived = true;
            aligned = true;
            target_set = false;
        }




        if (operate)
        {
            // Dati aggiornati dal simulatore del robot
            position_current = robot.position;
            orientation_current = robot.orientation;
            velocity_current = robot.linearVelocity.z;
            omega_current = robot.angularVelocity.y;


            // DATA PATH. Gestione azioni all'interno degli stati
            switch (STATE_M)
            {
                // Stato 0: Idle
                // Resta in attesa di un evento "target", e ricava i dati necessari alla pianificazione del moto
                case 0:
                    if (target_set)
                    {
                        // Aggiorna parametri di viaggio
                        travel_t = goal[3];
                        travel_d = Mathf.Sqrt(Mathf.Pow(goal[0] - position_current.x, 2) + Mathf.Pow(goal[1] - position_current.z, 2));

                        // Muoversi avanti o indietro?
                        if (reverse_movement_enabled)
                        {
                            if (goal[0] < position_current.x)
                                direction = -1;
                            else
                                direction = 1;
                        }
                        else
                            direction = 1;

                        // Check distanza
                        if (travel_d > 0.5f)
                        {
                            arrived = false;
                            // Calcola errore di allineamento tra orientamento robot e vettore distanza robot->goal
                            orientation_setpoint = Mathf.Rad2Deg * Mathf.Atan2(goal[0] - position_current.x, goal[1] - position_current.z);

                            // In caso di retromarcia l'allineamento deve essere in senso contrario
                            if (reverse_movement_enabled && (direction < 0))
                                orientation_setpoint += 180;
                        }
                        else
                        {
                            arrived = true;
                            orientation_setpoint = goal[2];
                        }

                        if (Mathf.Abs(orientation_setpoint - orientation_current) > orientation_epsilon)
                            aligned = false;
                        else
                            aligned = true;

                        // Aggiorna lo stato
                        updateState();
                    }
                    break;


                // Stato 1: Allineamento iniziale
                // Ruota sul posto verso la direttrice posizione(robot)--->posizione(goal)
                // Passaggio di stato gestito nella funzione chiamata
                case 1:
                    alignTime += Time.fixedDeltaTime;
                    base_controller.angular_velocity_setpoint = QuickTurn(turn_speed);
                    break;


                // Stato 2: Transito verso il goal
                // Assegna ed applica al controllore della base un profilo trapezoidale in velocità. Distizione tra le tre fasi
                case 2:
                    if (currentTime < accel_t)
                    {
                        // Accelerazione
                        velocity_target = (cruise_v * currentTime) / accel_t * direction;
                    }
                    else if (currentTime >= accel_t && currentTime <= (accel_t + cruise_t))
                    {
                        // Velocità di crociera
                        velocity_target = cruise_v * direction;
                    }
                    else if (currentTime > (accel_t + cruise_t) && currentTime <= goal[3])
                    {
                        // Decelerazione
                        velocity_target = (cruise_v - (cruise_v * (currentTime - cruise_t - accel_t) / accel_t)) * direction;
                    }
                    else
                    {
                        // Arrivati
                        //     Debug.Log("ARRIVATI");
                        velocity_target = 0;
                        updateState();
                        arrived = true;
                    }

                    //      Debug.Log("VELOCITA " + velocity_target);

                    // Applica le velocità candidate, compresa quella di allineamento per compensare deviazioni
                    base_controller.linear_velocity_setpoint = velocity_target;
                    angle_direction = ShortestAngle();
                    omega_target = kp * angle_direction;
                    base_controller.angular_velocity_setpoint = omega_target;
                    currentTime += Time.fixedDeltaTime;
                    break;

                            
                // Stato 3: Allineamento finale
                // Applica una velocità rotazionale per allinearsi all'obbiettivo iniziale
                case 3:
                    orientation_setpoint = goal[2];
                    base_controller.angular_velocity_setpoint = QuickTurn(turn_speed);
                    break;

                case 4: //Stato manuale
                    if (Input.GetKey("w"))
                    {
                        base_controller.linear_velocity_setpoint += 0.5f;                     
                    }

                    

                    if (Input.GetKey("s"))
                    {
                        base_controller.linear_velocity_setpoint -= 0.5f;                   
                    }

                    base_controller.linear_velocity_setpoint *= 0.9f;

                    if (Input.GetKey("a"))
                    {
                        //gira a sinistra
                        base_controller.angular_velocity_setpoint = -3f;
                    }

                    if (Input.GetKey("d"))
                    {
                        //gira a destra
                        base_controller.angular_velocity_setpoint = 3f;
                    }

                    base_controller.angular_velocity_setpoint *= 0.9f;

                    break;

         



                // Stato pozzo
                default:
                    Reset();
                    break;
            }
        }

        else
        {
            base_controller.linear_velocity_setpoint = 0;
            base_controller.angular_velocity_setpoint = 0;
        }
    }



}
