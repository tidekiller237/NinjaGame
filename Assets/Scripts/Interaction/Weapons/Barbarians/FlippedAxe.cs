using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlippedAxe : MonoBehaviour
{
    private void Start()
    {
        transform.localEulerAngles += new Vector3(0f, 0f, 180f);
    }
}
