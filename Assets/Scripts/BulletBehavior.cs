using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    public float speed = 5.0f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        rb.velocity = transform.forward * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(this.gameObject);
    }
}
