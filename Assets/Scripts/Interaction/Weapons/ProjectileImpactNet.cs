using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ProjectileImpactNet : MonoBehaviour
{
    public LayerMask environmentHitLayer;

    public BoxCollider environmentHitbox;

    Rigidbody rb;
    Vector3 lastPos;

    public bool targetHit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPos = transform.position;
        targetHit = false;
    }

    private void Update()
    {
        CheckForCollision();
    }

    private void CheckForCollision()
    {
        if (targetHit) return;

        RaycastHit hit;

        Physics.BoxCast(lastPos, environmentHitbox.size / 2, rb.velocity.normalized, out hit, transform.rotation, Vector3.Distance(lastPos, transform.position), environmentHitLayer);
        lastPos = transform.position;
        
        if(hit.collider != null)
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            GetComponent<Collider>().enabled = false;
            transform.position = hit.point;
            foreach (Collider col in GetComponentsInChildren<Collider>())
                col.enabled = false;

            targetHit = true;
        }
    }
}
