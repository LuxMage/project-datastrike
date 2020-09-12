using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.Types;
using System.Threading;
using DatastrikeNetwork;

public class RoleDistributor : MonoBehaviour
{
    public GameObject agent;
    public GameObject hacker;
    public GameObject startCamera;

    private bool toggle = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && !toggle)
        {
            BecomeAgent();
            toggle = true;
        }

        else if (Input.GetKeyDown(KeyCode.X) && !toggle)
        {
            BecomeHacker();
            toggle = true;
        }
    }

    public void BecomeAgent()
    {
        agent.GetComponent<NetworkIdentity>().localPlayerOwns = true;
        startCamera.SetActive(false);
        agent.transform.GetChild(0).gameObject.SetActive(true);

        Thread server = new Thread(new ThreadStart(NetworkCommunicator.RunHost));
        server.Start();
    }

    public void BecomeHacker()
    {
        hacker.GetComponent<NetworkIdentity>().localPlayerOwns = true;
        startCamera.SetActive(false);
        hacker.transform.GetChild(0).gameObject.SetActive(true);

        Thread client = new Thread(new ThreadStart(NetworkCommunicator.RunClient));
        client.Start();
    }
}
