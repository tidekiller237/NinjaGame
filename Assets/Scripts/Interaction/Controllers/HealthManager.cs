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
    public UnityEvent OnDeath;
    public UnityEvent OnBodyCleanUp;

    private void Awake()
    {
        maxHealth = GameManager.Instance.playerHealth;
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
                    Invoke(nameof(CleanUpBody), timeToRespawn / 2);
                    Invoke(nameof(Respawn), timeToRespawn);
                    //NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.transform.localScale = new(1f, 0.25f, 1f);
                    GetComponent<PlayerController>().weaponHolder.GetComponent<Weapon>().SetVisual(false);
                    OnDeath.Invoke();
                }

                //if (rb.freezeRotation)
                //{
                //    rb.freezeRotation = false;
                //    rb.useGravity = true;
                //}
            }
            else
            {
                if (respawning)
                    respawning = false;

                //if (!rb.freezeRotation)
                //{
                //    rb.freezeRotation = true;
                //    rb.useGravity = false;
                //}
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

    public void CleanUpBody()
    {
        if (!IsOwner) return;

        ulong clientId = NetworkManager.Singleton.LocalClientId;

        SetBodyActiveServerRpc(clientId, false);

        transform.GetChild(0).gameObject.SetActive(false);

        //perform movement while invisible
        GetComponent<SpawnHandler>().SpawnAtRandom();

        OnBodyCleanUp.Invoke();
    }

    public void Respawn()
    {
        if (!IsOwner) return;

        ulong clientId = NetworkManager.Singleton.LocalClientId;
        GetComponent<PlayerController>().mainCamera.OverrideRotation(transform.eulerAngles.x, transform.eulerAngles.y);

        //TODO: Add logic for intellegent spawn selection
        ResetHealth();
        GetComponent<PlayerController>().weaponHolder.GetComponent<Weapon>().SetVisual(true);

        SetBodyActiveServerRpc(clientId, true);

        transform.GetChild(0).gameObject.SetActive(true);
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

    [ServerRpc]
    private void SetBodyActiveServerRpc(ulong clientId, bool active)
    {
        SetBodyActiveClientRpc(clientId,  active);
    }

    [ClientRpc]
    private void SetBodyActiveClientRpc(ulong clientId, bool active)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId) return;

        transform.GetChild(0).gameObject.SetActive(active);
    }
}
