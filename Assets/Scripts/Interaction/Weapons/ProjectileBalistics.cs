using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBalistics : MonoBehaviour
{
    Rigidbody rb;

    public Transform visualComponent;
    public float visualSpeed;

    Vector3 startPos;
    Vector3 currentPos;
    float t;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        t = 0;
    }

    private void Update()
    {
        if(!rb.isKinematic && t <= 1f)
        {
            Vector3 posDelta = currentPos - transform.position;

            Vector3 upDelta = Vector3.Project(posDelta, transform.up);
            Vector3 rightDelta = Vector3.Project(posDelta, transform.right);

            upDelta = Vector3.Lerp(upDelta, Vector3.zero, Mathf.Min(1, t));
            rightDelta = Vector3.Lerp(rightDelta, Vector3.zero, Mathf.Min(1, t));

            currentPos = transform.position + upDelta + rightDelta;

            t += Time.deltaTime * visualSpeed;
        }
        else
            currentPos = transform.position;

        visualComponent.position = currentPos;
    }

    public void SetSpawnPosition(Vector3 position)
    {
        currentPos = position;
        startPos = position;
        visualComponent.position = startPos;
        visualComponent.GetComponentInChildren<TrailRenderer>().enabled = true;
    }
}
