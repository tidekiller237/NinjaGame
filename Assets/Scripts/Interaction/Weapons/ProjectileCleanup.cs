using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProjectileCleanup : NetworkBehaviour
{
    public float lifeTime;

    private void Awake()
    {
        Invoke(nameof(CleanUp), lifeTime);
    }

    private void CleanUp()
    {
        if (!IsOwner) return;
        CleanUpServerRPC();
    }

    [ServerRpc]
    private void CleanUpServerRPC()
    {
        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }
}
