using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ThrowingAxeNet : NetworkBehaviour
{
    public Transform axe;
    public float rotationsPerSecond;
    public Vector3 rotationAxis;
    public Vector3 localPosition;
    public Vector3 localRotation;
    float lastTickTime;

    private void Update()
    {
        if (!IsServer) return;

        if (GetComponent<Rigidbody>().velocity.magnitude > 0)
        {
            axe.transform.Rotate(rotationAxis, rotationsPerSecond * 360f * Time.deltaTime);
            UpdateRotationClientRpc();
        }
        else
        {
            axe.transform.localPosition = localPosition;
            axe.transform.localRotation = Quaternion.Euler(localRotation);
            OnStickClientRpc();
        }
    }

    [ClientRpc]
    private void UpdateRotationClientRpc()
    {
        axe.transform.Rotate(rotationAxis, rotationsPerSecond * 360f * (Time.time - lastTickTime));

        //StopAllCoroutines();
        //StartCoroutine(SmoothRotate());
        lastTickTime = Time.time;
    }

    [ClientRpc]
    private void OnStickClientRpc()
    {
        axe.transform.localPosition = localPosition;
        axe.transform.localRotation = Quaternion.Euler(localRotation);
    }

    private IEnumerator SmoothRotate()
    {
        float t = 0;
        Quaternion start = axe.transform.rotation;
        axe.transform.Rotate(rotationAxis, rotationsPerSecond * 360f * (Time.time - lastTickTime));
        Quaternion end = axe.transform.rotation;
        axe.transform.rotation = start;

        while(t <= 1)
        {
            axe.transform.rotation = Quaternion.Slerp(start, end, Mathf.Min(1, t));
            t += Time.deltaTime;
            yield return null;
        }

        axe.transform.rotation = end;
    }
}
