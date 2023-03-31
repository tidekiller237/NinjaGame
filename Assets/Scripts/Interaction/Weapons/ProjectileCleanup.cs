using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProjectileCleanup : NetworkBehaviour
{
    public float lifeTime;
    public Weapon weapon;

    private void Awake()
    {
        Invoke(nameof(CleanUp), lifeTime);
    }

    private void CleanUp()
    {
        if (!IsOwner) return;

        if (GetComponent<NetworkObject>() != null)
            CleanUpServerRPC();
        else
            weapon.DestroyLocalProjectile(gameObject);
    }

    [ServerRpc]
    private void CleanUpServerRPC()
    {
        GetComponent<NetworkObject>().Despawn();
    }
}
