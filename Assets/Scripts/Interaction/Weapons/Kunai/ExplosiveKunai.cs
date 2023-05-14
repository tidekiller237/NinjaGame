using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExplosiveKunai : MonoBehaviour
{
    public UnityEvent<GameObject> OnTrigger = new UnityEvent<GameObject>();

    public void SetTrigger(float triggerTime)
    {
        Invoke(nameof(Trigger), triggerTime);
    }

    private void Trigger()
    {
        OnTrigger.Invoke(gameObject);
    }
}
