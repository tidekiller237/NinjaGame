using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Weapon : NetworkBehaviour
{
    public bool Activated { private get; set; }

    protected Dictionary<int, GameObject> projDict;

    protected virtual void Awake()
    {
        Activated = false;
        projDict = new Dictionary<int, GameObject>();
    }

    protected virtual void Update()
    {
        enabled = Activated;
    }

    public void DestroyLocalProjectile(GameObject projectile)
    {
        if (projDict.ContainsKey(projectile.GetInstanceID()))
        {
            projDict.Remove(projectile.GetInstanceID());
            Destroy(projectile);
        }
    }
}
