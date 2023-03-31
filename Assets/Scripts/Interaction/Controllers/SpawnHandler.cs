using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(HealthManager))]
public class SpawnHandler : MonoBehaviour
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
        transform.position = point;
    }

    public void SpawnAtClosestTo(Vector3 point)
    {
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
    }

    public void SpawnAtRandom()
    {
        Vector3 spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
        transform.position = spawnPoint;
    }
}
