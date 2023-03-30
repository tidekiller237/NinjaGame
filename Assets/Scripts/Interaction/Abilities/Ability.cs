using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parent ability class.
/// </summary>
public class Ability : MonoBehaviour
{
    public enum AbilityType
    {
        Movement = 0
    }

    public bool Activated { private get; set; }
    public AbilityType Type;
    public KeyCode input;

    protected virtual void Awake()
    {
        Activated = false;
    }

    protected virtual void Update()
    {
        enabled = Activated;
    }
}
