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



[Serializable]
public struct hitCompactStr
{
    public short v0x;           // 100
    public short v0y;           // 100
    public short p0y;           // 1000 
    public ushort d1;           // 1000
    public ushort d2;           // 1000
    public ushort d3;           // 1000
    public ushort t3;           // 1
    public short sliceOffset;   // 1000
    public short sliceAngle;    // 1000
    public bool hitCounter;     // 1
};



public class TableHit
{
    hitCompactStr[, , , ,] tablePointer = new hitCompactStr[26, 7, 61, 9, 5];
    hitCompactStr hitStr;

    float fps, MinSpin, MaxSpin, MinSlice, MaxSlice;
    float Frame;



    public void Setup()
    {
        // Read file from C++
        string line;
        int counter = 0;
        System.IO.StreamReader file = new System.IO.StreamReader("Aligned_Indices.txt");
        char[] sep = { '\t' };
        while ((line = file.ReadLine()) != null)
        {
            string[] words = line.Split(sep, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length != 15)
                break;
            else
            {
                try
                {
                    hitStr.v0x = short.Parse(words[5]);
                    hitStr.v0y = short.Parse(words[6]);
                    hitStr.p0y = short.Parse(words[7]);
                    hitStr.d1 = ushort.Parse(words[8]);
                    hitStr.d2 = ushort.Parse(words[9]);
                    hitStr.d3 = ushort.Parse(words[10]);
                    hitStr.t3 = ushort.Parse(words[11]);
                    hitStr.sliceOffset = short.Parse(words[12]);
                    hitStr.sliceAngle = short.Parse(words[13]);
                    if (int.Parse(words[14]) == 1)
                    {
                        hitStr.hitCounter = true;
                        counter++;
                    }
                    else
                        hitStr.hitCounter = false;
                    tablePointer[int.Parse(words[0]), int.Parse(words[1]), int.Parse(words[2]), int.Parse(words[3]), int.Parse(words[4])] = hitStr;
                }
                catch (FormatException e)
                {
                    Debug.Log("Error during setup : " + e);
                }
            }
        }
        file.Close();

        IFormatter formatter = new BinaryFormatter();
        Stream stream = new FileStream("SimuSharp.dat", FileMode.Create, FileAccess.Write, FileShare.Read);
        formatter.Serialize(stream, tablePointer);
        stream.Close();


        Debug.Log("Wrote " + counter + " entries in SimuSharp.dat");
    }



    public int Init()
    {
        // Controllare che questi valori siano identici a quelli con cui è stata generata la tabella tramite Spanspace!!!
        fps = 30.0f;
        MinSpin = -5000;
        MaxSpin = 5000;
        MinSlice = -1000;
        MaxSlice = 1000;

        try
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("SimuSharp.dat", FileMode.Open, FileAccess.Read, FileShare.Read);
            tablePointer = (hitCompactStr[, , , ,])formatter.Deserialize(stream);
            stream.Close();
            return 0;
        }
        catch (FileNotFoundException f)
        {
            Debug.Log("File not found: " + f);
            Setup();
            return 1;
        }
    }



    public hitCompactStr GetHit(float gDist, float hImpact, float timeFlight, float spin, float slice)
    {
        Frame = timeFlight * fps;

        float Xqf = (25) * gDist / 25.0f;
        float Hqf = (6) * hImpact / 3.0f;
        float Tqf = (60) * Frame / 60.0f;
        float SPqf = (8) * (spin - MinSpin) / (MaxSpin - MinSpin);
        float SLqf = (4) * (slice - MinSlice) / (MaxSlice - MinSlice);

        int Xq = (int)(Xqf + 0.5f);
        int Hq = (int)(Hqf + 0.5f);
        int Tq = (int)(Tqf + 0.5f);
        int SPq = (int)(SPqf + 0.5f);
        int SLq = (int)(SLqf + 0.5f);

        if (Xq > 25)
            Xq = 25;

        if (Hq > 6)
            Hq = 6;

        if (Tq > 60)
            Tq = 60;

        if (SPq > 8)
            SPq = 8;

        if (SLq > 4)
            SLq = 4;

        hitStr = tablePointer[Xq, Hq, Tq, SPq, SLq];

        return hitStr;
    }
}