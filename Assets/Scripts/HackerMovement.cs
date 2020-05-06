using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HackerMovement : MonoBehaviour
{
    public float speed = 0.1f;
    public float sensitivity = 1.0f;
    public float sphereCastRadius = 0.35f;

    public GameObject cameraRig = null;

    private CharacterController charController = null;
    private GameObject cmra;
    private GameObject originalCameraPosition;

    private void Start()
    {
        charController = GetComponent<CharacterController>();
        cmra = cameraRig.transform.GetChild(0).gameObject;
        originalCameraPosition = cameraRig.transform.GetChild(1).gameObject;

        originalCameraPosition.transform.LookAt(transform);
    }

    private void Update()
    {
        Movement();
        CameraRotation();
    }

    private void Movement()
    {
        float horiz = Input.GetAxis("Horizontal") * speed;
        float vert = Input.GetAxis("Vertical") * speed;
        float altitude = (Input.GetAxis("Jump") - Input.GetAxis("Crouch")) * speed;

        Vector3 movement = new Vector3(horiz, 0.0f, vert);

        movement = transform.TransformDirection(movement);
        movement = movement + (Vector3.up * altitude); 

        charController.Move(movement * Time.deltaTime);
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
