using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;



public class DemoSetup : MonoBehaviour
{
    public bool operate, demo_running, cleaning, clean_trigger, spreadsheet, launch_OK, manual;
    public int STATE_D, launches;
    public float spin, slice;


    public BasicMotionPlanner planner;
    public CleaningPlanner cleaner;
    public PickupPlanner picker;

    public RobotLauncher launcher;

    public RobotSim robot;

    public Vector3 Plaunch;
    public Vector3 Planding;

    //int cycles;
    float shotDistance, launcher_height, crossDist, target_theta, flightTime, travelTime, travelDistance;

    public hitCompactStr hit, hitRec;
    public TableHit sSpace;

    //float counter;
    int maxLaunches = 1;

    // Contenitore dal *.csv
    public List<float[]> command_list;
    public int command_index;


    void Start()
    {
        // Local setup
        STATE_D = 0;
        //launches = cycles = 0;
        operate = false;
        demo_running = false;
        Plaunch.Set(0, 0, 0);
        Planding.Set(0, 0, 0);
        flightTime = 0;
        travelTime = 0;
        shotDistance = 0;
        travelDistance = 0;
        target_theta = 0;
        launcher_height = 0.4f;
        launch_OK = false;
        //counter = 0;
        clean_trigger = false;
        manual = false;

        //launcher.enabled = false;
        //launcher.enabled = true;

        // CSV reader setup
        command_list = new List<float[]>();
        command_list.Clear();
        command_index = 0;
        spreadsheet = false;
        LoadSpreadSheet();

        // Planner setup
        planner.operate = true;
        planner.reverse_movement_enabled = true;

        // Cleaner setup
        cleaner.operate = true;

        // Tablehit setup
        sSpace = new TableHit();
        sSpace.Init();

    }



    public void ClearStruct()
    {
        hit.v0x = 0;
        hit.v0y = 0;
        hit.p0y = 0;
        hit.d1 = 0;
        hit.d2 = 0;
        hit.d3 = 0;
        hit.t3 = 0;
        hit.sliceOffset = 0;
        hit.sliceAngle = 0;
        hit.hitCounter = false;
    }

    public void Play()
    {
        this.operate = true;
        this.demo_running = true;
    }

    public void ReStart()
    {
        STATE_D = 0;
        planner.operate = true;
        this.demo_running = true;
        manual = false;
        planner.Reset();
    }

    public void Clean()
    {
        if (!cleaner.operate)
            this.clean_trigger = true;
        else
        {
            cleaner.operate = false;
            this.cleaning = false;
        }
    }


    void LoadSpreadSheet()
    {
        float[] values;
        string line;
        values = new float[11];
        char[] sep = { ',' };
        int lineCount = 0;

        try
        {
            System.IO.StreamReader file = new System.IO.StreamReader("Sequence - Sequence1.csv");

            while ((line = file.ReadLine()) != null)
            {
                if (lineCount > 1)
                {
                    string[] words = line.Split(sep);
                    for (int i = 1; i < 11; ++i)
                    {
                        try
                        {
                            values[i] = float.Parse(words[i]);
                        }
                        catch (FormatException e)
                        {
                            Debug.Log(e + "\t,\t" + words[i]);
                        }
                    }
                    command_list.Add((float[])values.Clone());

                }
                lineCount++;
            }
            file.Close();
        }
        catch (FileNotFoundException e)
        {
            Debug.Log(e);
        }
    }



    void updateState()
    {

        switch (STATE_D)
        {
            case 0:
                if (clean_trigger)
                {
                    cleaning = true;
                    planner.reverse_movement_enabled = false;
                    cleaner.operate = true;
                    cleaner.clean_field = true;
                    clean_trigger = false;
                    STATE_D = 4;
                }
                else
                {
                    launcher.launch_event = false;
                    planner.target_set = true;
                    STATE_D = 1;
                }
                break;


            case 1:
                if (clean_trigger)
                {
                    cleaning = true;
                    planner.reverse_movement_enabled = false;
                    cleaner.operate = true;
                    cleaner.clean_field = true;
                    clean_trigger = false;
                    STATE_D = 4;
                }
                else
                    STATE_D = 2;
                break;


            case 2:
                if (clean_trigger)
                {
                    cleaning = true;
                    planner.reverse_movement_enabled = false;
                    cleaner.operate = true;
                    cleaner.clean_field = true;
                    clean_trigger = false;
                    STATE_D = 4;
                }
                else if (launches == maxLaunches)
                {
                    planner.reverse_movement_enabled = false;
                    picker.operate = true;
                    picker.pickup = true;
                    STATE_D = 3;
                }
                else
                {
                    if (spreadsheet)
                    {
                        command_index++;
                        if (command_index >= command_list.Count)
                            command_index = 0;
                    }
                    STATE_D = 0;
                }
                break;

            case 3:
                // ritorna allo stato 0, resetta i lanci, ricomincia
                planner.reverse_movement_enabled = true;
                launches = 0;
                STATE_D = 0;
                break;

            case 4:
                cleaner.operate = false;
                planner.reverse_movement_enabled = true;
                STATE_D = 0;
                break;

            case 5:
                cleaning = false;
                clean_trigger = false;
                demo_running = false;
                break;
        }

    }



    void FixedUpdate()
    {
        if (Input.GetKey("k"))
        {
            STATE_D = 5;
            manual = true;
        }
        if (operate)
        {

            switch (STATE_D)
            {

                case 0:
                    if (clean_trigger)
                    {
                        demo_running = false;
                        planner.Reset();
                        // Triggera la fase di pulizia
                        updateState();
                    }

                    // In attesa, genera il tiro
                    if (demo_running)
                    {
                        cleaning = false;

                        if (spreadsheet)
                        {

                            Plaunch.Set(command_list.ElementAt(command_index)[3], 0, command_list.ElementAt(command_index)[4]);
                            Planding.Set(command_list.ElementAt(command_index)[5], 0, command_list.ElementAt(command_index)[6]);
                            flightTime = 2 - (1.5f * (command_list.ElementAt(command_index)[7] / 100.0f));
                            spin = command_list.ElementAt(command_index)[9];
                            slice = command_list.ElementAt(command_index)[10];
                            travelTime = command_list.ElementAt(command_index)[1];
                            travelDistance = Mathf.Sqrt(Mathf.Pow(robot.position.x - Plaunch.x, 2) + Mathf.Pow(robot.position.z - Plaunch.z, 2));
                        }
                        else
                        {
                            // Genera lancio random;
                            // Parametri:
                            // lancio(X Z),       range: [-4 4] [-12 -1]
                            // atterraggio(X Z),  range: [4 4]  [1  11.89]
                            // tempo di volo(s),  [0.75 2]
                            // tempo in moto,     dipendente dalla distanza
                            // spin,              [-5000 5000]
                            // slice,             [-1000 1000]

                            Plaunch.x = (UnityEngine.Random.value * 8) - 4;
                            Plaunch.y = 0;
                            Plaunch.z = (UnityEngine.Random.value * 11) - 12;
                            Planding.x = (UnityEngine.Random.value * 8) - 4;
                            Planding.y = 0;
                            Planding.z = (UnityEngine.Random.value * 10.89f) + 1;
                            flightTime = (UnityEngine.Random.value * 1.25f) + 0.75f;

                            // 75% di prob. di tempo di volo corto
                            if (UnityEngine.Random.value > 0.5f)
                                flightTime = (UnityEngine.Random.value * 0.7f) + 0.8f;
                            else
                                flightTime = (UnityEngine.Random.value * 1.25f) + 0.75f;

                            // 20% di prob. di spin o slice non nulli (se il Random di Unity è ugualmente distribuito)
                            if (UnityEngine.Random.value > 0.8f)
                                spin = (UnityEngine.Random.value * 10000.0f) - 5000.0f;
                            else
                                spin = 0;

                            if (UnityEngine.Random.value > 0.8f)
                                slice = (UnityEngine.Random.value * 2000.0f) - 1000.0f;
                            else
                                slice = 0;

                            travelDistance = Mathf.Sqrt(Mathf.Pow(robot.position.x - Plaunch.x, 2) + Mathf.Pow(robot.position.z - Plaunch.z, 2));
                            travelTime = travelDistance / 2.0f;
                        }

                        // Pulisci la struttura
                        ClearStruct();

                        // Interroga la tabella
                        shotDistance = Mathf.Sqrt(Mathf.Pow(Planding.x - Plaunch.x, 2) + Mathf.Pow(Planding.z - Plaunch.z, 2));
                        target_theta = Mathf.Rad2Deg * Mathf.Atan2(Planding.x - Plaunch.x, Planding.z - Plaunch.z);
                        crossDist = Mathf.Abs(Plaunch.z) / Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * target_theta));

                        // Query sull'array
                        hit = sSpace.GetHit(shotDistance, launcher_height, flightTime, spin, slice);

                        // Controllo validità lancio
                        if ((hit.hitCounter == false) || (crossDist < (float)(hit.d1 / 1000.0f) || (crossDist > (float)(hit.d2 / 1000.0f))))
                            launch_OK = false;
                        else
                            launch_OK = true;

                        // Correzione angolo dovuta a slice non nullo
                        if (slice != 0)
                            target_theta = target_theta - Mathf.Rad2Deg * (float)(hit.sliceAngle / 1000.0f);

                        // Passa i parametri al planner (NOTA: +90° per far lanciare di lato il robot
                        planner.goal.Set(Plaunch.x - Mathf.Sin(target_theta) * 0.2f,
                                          Plaunch.z - Mathf.Cos(target_theta) * 0.2f,
                                          target_theta + 90, travelTime);
                        updateState();
                    }
                    break;


                case 1:

                    // Triggera la fase di pulizia
                    if (clean_trigger)
                    {
                        demo_running = false;
                        planner.Reset();
                        updateState();
                    }

                    // In moto; non far nulla
                    if (planner.arrived && planner.aligned && !planner.target_set)
                        updateState();
                    break;


                case 2:
                    // Triggera la fase di pulizia
                    if (clean_trigger)
                    {
                        demo_running = false;
                        planner.Reset();
                        updateState();
                    }

                    // Setup di lancio
                    if (launch_OK)
                    {
                        launcher.initialVelocity.x = ((float)(hit.v0x / 100.0f)) * Mathf.Sin(Mathf.Deg2Rad * target_theta);
                        launcher.initialVelocity.y = ((float)(hit.v0y / 100.0f));
                        launcher.initialVelocity.z = ((float)(hit.v0x / 100.0f)) * Mathf.Cos(Mathf.Deg2Rad * target_theta);
                        launcher.initialSlice = slice;
                        launcher.initialSpin = spin;
                        launcher.initialPose = robot.components[3].transform.position;
                        launcher.initialPose.y = launcher_height;

                        // Triggera il lancio
                        launcher.launch_event = true;
                        launches++;
                    }
                    updateState();
                    break;

                case 3:
                    // Aspetta che prenda le palline finché non ha finito
                    if (!picker.operate)
                        updateState();
                    break;


                case 4:
                    // Fase di pulizia. Aspetta che abbia finito
                    if (!cleaner.operate && !cleaner.clean_field && !cleaner.clean_lines)
                    {
                        cleaning = false;
                    }

                    // Interruzione della procedura: torniamo allo stato 0
                    if (!cleaning)
                        updateState();
                    break;

                case 5:
                    demo_running = false;
                    clean_trigger = false;
                    cleaning = false;
                    updateState();
                    break;


                default:
                    break;

            }
        }
        else
        {
            if (demo_running)
                demo_running = false;
            if (cleaning)
                cleaning = false;
        }

    }

}



