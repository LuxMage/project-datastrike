using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DatastrikeNetwork;

public class HackerMovement : MonoBehaviour
{
    public float speed = 0.1f;
    public float sensitivity = 1.0f;
    public float sphereCastRadius = 0.35f;

    public GameObject cameraRig = null;

    private CharacterController charController = null;
    private GameObject cmra;
    private GameObject originalCameraPosition;
    private NetworkIdentity netId;

    private Vector3 lastPos;
    private Vector3 lastRot;
    private Vector3 newPos;
    private Vector3 newRot;
    private float lerpRatio = 1 / (float)NetworkClock.modulus;
    private float currentLerp = 1 / (float)NetworkClock.modulus;

    private void Start()
    {
        charController = GetComponent<CharacterController>();
        cmra = cameraRig.transform.GetChild(0).gameObject;
        originalCameraPosition = cameraRig.transform.GetChild(1).gameObject;
        netId = GetComponent<NetworkIdentity>();

        originalCameraPosition.transform.LookAt(transform);

        lastPos = transform.position;
        lastRot = transform.eulerAngles;
        newPos = transform.position;
        newRot = transform.eulerAngles;
    }

    private void Update()
    {
        if (netId.localPlayerOwns)
        {
            Movement();
            CameraRotation();
            if (NetworkClock.IsTimeToSend())
            {
                netId.SendDataOverNetwork(NetworkEventType.UpdatePosition, NetworkSubeventType.Null, transform);
            }
        }

        else
        {
            transform.position = Vector3.Lerp(lastPos, newPos, currentLerp);
            transform.eulerAngles = Vector3.Lerp(lastRot, newRot, currentLerp);
            currentLerp += lerpRatio;

            while (netId.dataQueue.Count != 0)
            {
                NetworkEvent currentData = netId.dataQueue[0];
                netId.dataQueue.RemoveAt(0);
                if (currentData.GetNetworkEventType() == NetworkEventType.UpdatePosition)
                {
                    Vector3[] newTrans = (Vector3[])currentData.GetData();
                    lastPos = new Vector3(newPos.x, newPos.y, newPos.z);
                    lastRot = new Vector3(newRot.x, newRot.y, newRot.z);
                    newPos = newTrans[0];
                    newRot = newTrans[1];
                    currentLerp = lerpRatio;

                    transform.localScale = newTrans[2];
                }
            }
        }
    }

    private void Movement()
    {
        float horiz = Input.GetAxis("Horizontal") * speed;
        float vert = Input.GetAxis("Vertical") * speed;
        float altitude = (Input.GetAxis("Jump") - Input.GetAxis("Crouch")) * speed;

        Vector3 movement = new Vector3(horiz, 0.0f, vert);

        movement = transform.TransformDirection(movement);
        movement = movement + (Vector3.up * altitude);
        movement *= Time.deltaTime;

        charController.Move(movement);
    }

    private void CameraRotation()
    {
        float horiz = Input.GetAxis("Mouse X") * sensitivity;
        float vert = -Input.GetAxis("Mouse Y") * sensitivity;

        Vector3 rotation = new Vector3(vert, horiz, 0.0f);

        transform.Rotate(rotation);

        Quaternion q = transform.rotation;
        float qx = q.eulerAngles.x;
        float qy = q.eulerAngles.y;

        if (qx < 370.0f && qx >= 270.0f)
        {
            qx = Mathf.Clamp(qx, 290.0f, 370.0f);
        }

        else if (qx >= 0.0f && qx < 90.0f)
        {
            qx = Mathf.Clamp(qx, -5.0f, 80.0f);
        }

        q.eulerAngles = new Vector3(qx, qy, 0.0f);
        transform.rotation = q;

        RaycastHit hit;
        int layerMask = 1 << 8;
        layerMask = ~layerMask;

        if (Physics.SphereCast(transform.position, sphereCastRadius, -originalCameraPosition.transform.forward, out hit, Vector3.Distance(transform.position, originalCameraPosition.transform.position), layerMask))
        {
            float fullDist = Vector3.Distance(transform.position, originalCameraPosition.transform.position);
            float partDist = Vector3.Distance(transform.position, hit.point);
            float ratio = partDist / fullDist;
            ratio = Mathf.Clamp(ratio, 0.2f, 1.0f);

            cmra.transform.position = Vector3.Lerp(transform.position, originalCameraPosition.transform.position, ratio);
        }

        else
        {
            cmra.transform.position = originalCameraPosition.transform.position;
        }
    }
}
