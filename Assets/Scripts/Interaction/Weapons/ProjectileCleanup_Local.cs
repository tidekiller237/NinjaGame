using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileCleanup_Local : MonoBehaviour
{
    public float lifeTime;

    private void Awake()
    {
        Invoke(nameof(CleanUp), lifeTime);
    }

    private void CleanUp()
    {
        Destroy(gameObject);
    }
}
