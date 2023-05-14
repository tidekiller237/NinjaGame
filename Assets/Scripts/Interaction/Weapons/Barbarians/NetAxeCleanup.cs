using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetAxeCleanup : MonoBehaviour
{
    public float lifeTime;

    private void Update()
    {
        if (GetComponent<Rigidbody>().velocity.magnitude == 0)
        {
            if (lifeTime > 0)
                lifeTime -= Time.deltaTime;
            else
                Destroy(gameObject);
        }
    }
}
