using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProjectileCleanup : MonoBehaviour
{
    public float lifeTime;
    public Weapon weapon;

    private void Awake()
    {
        Invoke(nameof(CleanUp), lifeTime);
    }

    private void CleanUp()
    {
        weapon.DestroyProjectile(gameObject);
    }
}
