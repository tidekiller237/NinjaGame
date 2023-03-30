using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealthManager : NetworkBehaviour
{
    public int maxHealth;
    int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }
}
