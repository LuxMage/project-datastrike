using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DatastrikeNetwork;
using System;

public class NetworkIdentity : MonoBehaviour
{
    public bool localPlayerOwns = false;
    public List<NetworkEvent> dataQueue = new List<NetworkEvent>();

    private ushort networkId;
    private HashSet<NetworkEventType> validEvents;
    [SerializeField]
    private List<NetworkEventType> validEventsList = null;

    private void Awake()
    {
        networkId = NetworkSerializer.Register(this);
        validEvents = new HashSet<NetworkEventType>(validEventsList);

        if (networkId == UInt16.MaxValue)
        {
            Debug.LogError(this.gameObject.ToString() + " failed network registration!");
        }
    }

    public int GetNetworkId()
    {
        return networkId;
    }

    public void SendDataOverNetwork(NetworkEventType eventType, NetworkSubeventType subeventType, object data)
    {
        NetworkSerializer.AssembleMessage(networkId, eventType, subeventType, data);
    }

    public HashSet<NetworkEventType> GetValidEvents()
    {
        return validEvents;
    }

    public static void ConsolePrint(string text)
    {
        Debug.Log(text);
    }
}
