using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(PlayerController), typeof(HealthManager))]
public class SpawnHandler : NetworkBehaviour
{
    List<Transform> spawnPoints;

    private void Awake()
    {
        spawnPoints = new List<Transform>();
        GameObject spawnPointParent = GameObject.Find("SpawnPointHolder");
        foreach (Transform child in spawnPointParent.transform)
            spawnPoints.Add(child);
    }

    public void SpawnAtPoint(Vector3 point)
    {
        if (!IsOwner) return;

        transform.position = point;
    }

    public void SpawnAtClosestTo(Vector3 point)
    {
        if (!IsOwner) return;

        float minDistance = float.MaxValue;
        int spawnPoint = 0;

        for(int i = 0; i < spawnPoints.Count; i++)
        {
            if(Vector3.Distance(point, spawnPoints[i].position) < minDistance)
            {
                minDistance = Vector3.Distance(point, spawnPoints[i].position);
                spawnPoint = i;
            }
        }

        transform.position = spawnPoints[spawnPoint].position;
        transform.rotation = spawnPoints[spawnPoint].rotation;
    }

    public void SpawnAtRandom()
    {
        if (!IsOwner) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        Invoke(nameof(UpdatePositionDelay), 1f);
    }

    public void UpdatePositionDelay()
    {
        transform.position += transform.forward;
    }

    [ServerRpc]
    private void SpawnPositionServerRpc(ulong clientId, Vector3 position)
    {
        SpawnPositionClientRpc(clientId, position);
    }

    [ClientRpc]
    private void SpawnPositionClientRpc(ulong clientId, Vector3 position)
    {
        if (OwnerClientId == clientId) return;

        transform.position = position;
    }
}
