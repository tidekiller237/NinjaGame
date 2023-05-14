using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Barbarian : Class
{
    public int Rage { get; private set; }
    public bool Raging { get; private set; }

    public int maxRage;

    public UnityEvent<int> onRageChanged = new UnityEvent<int>();
    public UnityEvent<int> onRageAdded = new UnityEvent<int>();
    public UnityEvent<int> onRageUsed = new UnityEvent<int>();
    public UnityEvent onRageEntered = new UnityEvent();
    public UnityEvent onRageExited = new UnityEvent();

    protected override void Awake()
    {
        base.Awake();

        Rage = 0;
    }

    protected override void Update()
    {
        base.Update();
    }

    #region Interface

    public void EnterRage()
    {
        Raging = true;
        onRageEntered.Invoke();
    }

    public void ExitRage()
    {
        Raging = false;
        onRageExited.Invoke();
    }

    public void AddRage(int amount)
    {
        Rage += amount;
        Rage = Mathf.Min(Rage, maxRage);
        onRageChanged.Invoke(Rage);
        onRageAdded.Invoke(amount);
    }

    public void UseRage(int amount)
    {
        Rage -= amount;
        Rage = Mathf.Max(Rage, 0);
        onRageChanged.Invoke(Rage);
        onRageUsed.Invoke(amount);
    }

    public void UseRage()
    {
        onRageChanged.Invoke(0);
        onRageUsed.Invoke(Rage);
        Rage = 0;
    }

    public bool CheckCost(int cost)
    {
        return Rage >= cost;
    }

    public bool CheckMaxCost()
    {
        return Rage == maxRage;
    }

    #endregion
}
