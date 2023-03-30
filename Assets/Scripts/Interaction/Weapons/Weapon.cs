using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Weapon : NetworkBehaviour
{
    public bool Activated { private get; set; }
    public KeyCode primaryInput;
    public KeyCode secondaryInput;
    public KeyCode tertiaryInput;
    public KeyCode reloadInput;
    public KeyCode swapInput;

    protected virtual void Awake()
    {
        Activated = false;
    }

    protected virtual void Update()
    {
        enabled = Activated;
    }
}
