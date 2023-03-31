using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ProjectileInpact : MonoBehaviour
{
    public UnityEvent<GameObject, Collider> OnImpact;
    public LayerMask hitLayer;

    //GameObject[] debugSpheres = new GameObject[2];

    Rigidbody rb;
    Vector3 lastPos;

    bool targetHit;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPos = transform.position;

    }

    private void Update()
    {
        CheckForCollision();
    }

    private void CheckForCollision()
    {
        if (targetHit) return;

        RaycastHit hit;
        Physics.CapsuleCast(lastPos + (rb.velocity.normalized * GetComponent<CapsuleCollider>().height / 2),
            transform.position - (rb.velocity.normalized * GetComponent<CapsuleCollider>().height / 2), 
            GetComponent<CapsuleCollider>().radius, (transform.position - lastPos).normalized, out hit, (transform.position - lastPos).magnitude, hitLayer);

        lastPos = transform.position;

        //return if hit nothing or if hit a dead player
        if (hit.collider == null ||
            (hit.collider.CompareTag("Player") && !hit.collider.GetComponent<HealthManager>().IsAlive)) return;

        targetHit = true;
        rb.velocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        transform.position = hit.point;
        OnImpact.Invoke(gameObject, hit.collider);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (targetHit) return;
        else targetHit = true;

        if(collision.rigidbody != null)
            collision.rigidbody.AddForce(collision.impulse, ForceMode.Impulse);

        //OnImpact.Invoke(gameObject, collision);
    }
}
