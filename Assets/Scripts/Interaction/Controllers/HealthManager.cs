using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class HealthManager : NetworkBehaviour
{
    public bool IsAlive { get; private set; }
    public int maxHealth;
    public float timeToRespawn;
    public bool respawning;
    public NetworkVariable<int> currentHealth;
    int lastHealth;
    Rigidbody rb;

    public UnityEvent<int> OnDamageTaken;
    public UnityEvent<int> OnHealthGained;
    public UnityEvent<int> OnHealthChanged;

    private void Awake()
    {
        IsAlive = true;
        currentHealth = new NetworkVariable<int>(maxHealth, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        lastHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        IsAlive = currentHealth.Value > 0;

        if (IsOwner)
        {
            if (lastHealth != currentHealth.Value)
            {
                int delta = lastHealth - currentHealth.Value;

                OnHealthChanged.Invoke(delta);
                if (delta < 0)
                    OnDamageTaken.Invoke(delta);
                else if (delta > 0)
                    OnHealthGained.Invoke(delta);

                lastHealth = currentHealth.Value;
            }

            if (!IsAlive)
            {
                if (!respawning)
                {
                    respawning = true;
                    Invoke(nameof(Respawn), timeToRespawn);
                    PlayerController.Instance.transform.localScale = new(1f, 0.25f, 1f);
                }

                if (rb.freezeRotation)
                {
                    rb.freezeRotation = false;
                    rb.useGravity = true;
                }
            }
            else
            {
                if (respawning)
                    respawning = false;

                if (!rb.freezeRotation)
                {
                    rb.freezeRotation = true;
                    rb.useGravity = false;
                }
            }
        }
    }

    public void Damage(int amount)
    {
        currentHealth.Value = (int)Mathf.Max(0f, currentHealth.Value - amount);
    }

    public void Heal(int amount)
    {
        currentHealth.Value = (int)Mathf.Min(currentHealth.Value + amount, maxHealth);
    }

    public void SetHealth(int amount)
    {
        currentHealth.Value = (int)Mathf.Clamp(amount, 0f, maxHealth);
    }

    public void Respawn()
    {
        if (!IsOwner) return;

        //TODO: respawn at random for now
        GetComponent<SpawnHandler>().SpawnAtRandom();
        ResetHealth();
        PlayerController.Instance.transform.localScale = Vector3.one;
    }

    public void ResetHealth()
    {
        if (!IsOwner) return;
        
        ResetHealthServerRpc();
    }

    [ServerRpc]
    private void ResetHealthServerRpc()
    {
        currentHealth.Value = maxHealth;
    }
}
