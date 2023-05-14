using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxeThrowSecondary : MonoBehaviour
{
    public bool local;

    public float triggerTime;

    public BoxCollider openCollider;
    public Transform[] axes;
    bool triggered;
    float time;

    public List<GameObject> hits = new List<GameObject>();

    private void Awake()
    {
        triggered = false;
    }

    private void Update()
    {
        if (triggerTime >= 0)
        {
            if (time < triggerTime)
                time += Time.deltaTime;
            else if (!triggered)
                Trigger();
        }
    }

    public void Trigger()
    {
        triggered = true;

        if(local)
            GetComponent<ProjectileImpact>().playerHitbox = openCollider;

        axes[0].position = axes[0].position + transform.right * -2;
        axes[1].position = axes[1].position + transform.right * -1;
        axes[3].position = axes[3].position + transform.right * 1;
        axes[4].position = axes[4].position + transform.right * 2;
    }

    public void AddHit(GameObject hit)
    {
        hits.Add(hit);
    }
}
