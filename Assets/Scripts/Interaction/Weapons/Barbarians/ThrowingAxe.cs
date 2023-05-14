using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowingAxe : MonoBehaviour
{
    public Transform axe;
    public float rotationsPerSecond;
    public Vector3 rotationAxis;
    public Vector3 localPosition;
    public Vector3 localRotation;

    public List<GameObject> hits = new List<GameObject>();

    private void Update()
    {
        if (transform.root.GetComponent<Rigidbody>().velocity.magnitude > 0)
        {
            axe.transform.Rotate(rotationAxis, rotationsPerSecond * 360f * Time.deltaTime);
        }
        else
        {
            axe.transform.localPosition = localPosition;
            axe.transform.localRotation = Quaternion.Euler(localRotation);
        }
    }

    public void AddHit(GameObject obj)
    {
        hits.Add(obj);
    }
}
