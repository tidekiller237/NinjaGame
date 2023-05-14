using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ProjectileImpact : MonoBehaviour
{
    public UnityEvent<GameObject, Collider, RaycastHit> OnImpact;

    [Header("Player Interaction")]
    public bool collideWithPlayers;
    public LayerMask playerHitLayer;
    public BoxCollider playerHitbox;

    [Header("Environment Interaction")]
    public bool collideWithWorld;
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

        RaycastHit playerHit = new RaycastHit();
        RaycastHit environmentHit = new RaycastHit();

        if(collideWithPlayers)
            Physics.BoxCast(lastPos, playerHitbox.size / 2, rb.velocity.normalized, out playerHit, transform.rotation, Vector3.Distance(lastPos, transform.position), playerHitLayer);
        
        if(collideWithWorld)
            Physics.BoxCast(lastPos, environmentHitbox.size / 2, rb.velocity.normalized, out environmentHit, transform.rotation, Vector3.Distance(lastPos, transform.position), environmentHitLayer);
        
        lastPos = transform.position;

        //return if hit nothing or if hit a dead player
        if (collideWithPlayers && playerHit.collider != null && playerHit.collider.transform.root.GetComponent<HealthManager>() && playerHit.collider.transform.root.GetComponent<HealthManager>().IsAlive)
        {
            OnImpact.Invoke(gameObject, playerHit.collider, playerHit);
        }
        else if(collideWithWorld && environmentHit.collider != null)
        {
            OnImpact.Invoke(gameObject, environmentHit.collider, environmentHit);
        }
    }
}
