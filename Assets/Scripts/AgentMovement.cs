using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentMovement : MonoBehaviour
{
    public float speed = 25.0f;
    public float sensitivity = 1.0f;
    public float jumpHeight = 25.0f;
    [Range(0.01f, 0.5f)]
    public float jumpFloatiness = 0.05f;
    public float sphereCastRadius = 0.35f;

    public GameObject bullet = null;
    public GameObject bulletSpawn = null;
    public GameObject cameraRig = null;

    private float jumpProgress = 0.0f;

    private Camera c;
    private CharacterController charController;
    private GameObject cmra;
    private GameObject originalCameraPosition;

    private void Start()
    {
        charController = GetComponent<CharacterController>();
        cmra = cameraRig.transform.GetChild(0).gameObject;
        originalCameraPosition = cameraRig.transform.GetChild(1).gameObject;
        c = cmra.GetComponent<Camera>();

        originalCameraPosition.transform.LookAt(transform);
    }

    private void Update()
    {
        Movement();
        CameraMovement();
        Shooting();
    }

    private void Movement()
    {
        float horiz = Input.GetAxis("Horizontal") * speed;
        float vert = Input.GetAxis("Vertical") * speed;

        Vector3 movement = new Vector3(horiz, 0.0f, vert);

        if (Input.GetButtonDown("Jump") && charController.isGrounded)
        {
            jumpProgress = jumpHeight;
            movement.y = jumpProgress;
        }

        else if (!charController.isGrounded)
        {
            jumpProgress -= 0.1f;
            movement.y = jumpProgress;
        }

        movement = cameraRig.transform.TransformDirection(movement);

        charController.Move(movement * Time.deltaTime - (Vector3.up * 0.01f));
    }

    private void CameraMovement()
    {
        float horiz = Input.GetAxis("Mouse X") * sensitivity;
        float vert = -Input.GetAxis("Mouse Y") * sensitivity;

        Vector3 crRotation = new Vector3(vert, 0.0f, 0.0f);
        Vector3 agRotation = new Vector3(0.0f, horiz, 0.0f);

        cameraRig.transform.Rotate(crRotation);
        transform.Rotate(agRotation);

        Quaternion crQ = cameraRig.transform.rotation;
        Quaternion agQ = transform.rotation;

        float qx = crQ.eulerAngles.x;
        float qy = agQ.eulerAngles.y;

        if (qx < 370.0f && qx >= 270.0f)
        {
            qx = Mathf.Clamp(qx, 290.0f, 370.0f);
        }

        else if (qx >= 0.0f && qx < 90.0f)
        {
            qx = Mathf.Clamp(qx, -5.0f, 80.0f);
        }

        crQ.eulerAngles = new Vector3(qx, qy, 0.0f);
        cameraRig.transform.rotation = crQ;
        agQ.eulerAngles = new Vector3(0.0f, qy, 0.0f);
        transform.rotation = agQ;

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

    private void Shooting()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Ray ray = c.ScreenPointToRay(new Vector3(c.pixelWidth / 2.0f, c.pixelHeight / 2.0f, 0));

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100000.0f, ~(1 << 8)))
            {
                GameObject b = GameObject.Instantiate(bullet, bulletSpawn.transform.position, Quaternion.identity);

                b.transform.LookAt(hit.point);
            }
        }
    }
}