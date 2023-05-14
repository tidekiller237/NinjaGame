using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using Unity.Collections;

public class Class : MonoBehaviour
{
    public bool Activated { private get; set; }
    public PlayerController controller { protected get; set; }

    protected virtual void Awake()
    {
        Activated = false;
    }

    protected virtual void Update()
    {
        enabled = Activated;
    }
}
