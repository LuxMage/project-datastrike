using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using DatastrikeNetwork;
using Open.Nat;

public class NetworkClock : MonoBehaviour
{
    public static int modulus = 4;

    private static bool communicationTrigger = false;
    private static int time = 1;

    // Runs 50 times a second
    private void FixedUpdate()
    {
        if (communicationTrigger)
        {
            time = (time + 1) % modulus;

            if (time == 0)
            {
                Thread s = new Thread(new ThreadStart(SendData));
                Thread r = new Thread(new ThreadStart(RecvData));
                s.Start();
                r.Start();
            }
        }
    }

    private static void SendData()
    {
        NetworkCommunicator.SendDataBuffer();
    }

    private static void RecvData()
    {
        NetworkCommunicator.RecvDataBuffer();
    }

    public static void StartCommunication()
    {
        communicationTrigger = true;
    }

    public static bool IsTimeToSend()
    {
        return ((time + 1) % modulus) == 0;
    }

    public static void ConsolePrint(string text)
    {
        Debug.Log(text);
    }
}
