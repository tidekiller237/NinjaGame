using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BarbarianWeapon : Weapon
{
    [Header("Ranged Weapon")]
    public GameObject rangedWeapon;
    public string rangedResourceDirectory;
    public string[] primaryProjectile_Local;
    public string[] primaryProjectile_Network;
    public string secondaryProjectile_Local;
    public string secondaryProjectile_Network;
    public LayerMask notPlayerMask;

    /// <summary>
    /// 0-1: Primary fire locations
    /// 2: Secondary fire location
    /// </summary>
    public Transform[] projectileSpawnPoint;

    [Header("Ranged Primary Fire")]
    public int primaryDamage;
    public int burstCount;
    public float primarySpawnCameraOffset;
    public float primaryForwardForce;
    public float primaryUpForce;
    public float primaryFireCooldown;       //time in between bursts
    public float burstRate;                 //projectiles per second
    public float primaryFireVisualResetTime;
    int burstFiredCount;
    bool bursting;
    bool nextBurst;
    bool canPrimaryFire;
    float primaryFireVisualResetTimer;
    bool primaryVisualResetting;

    [Header("Ranged Secondary Fire")]
    public int secondaryDamage;
    public float secondaryTriggerTime;
    public float secondarySpawnCameraOffset;
    public float secondaryForwardForce;
    public float secondaryUpForce;
    public float secondaryFireCooldown;
    [HideInInspector] public float secondaryFireCDTime;
    bool canSecondaryFire;

    [Header("Weapon Swap")]
    public float attackAfterSwap;

    protected void OnAwake()
    {
        base.Awake();
        melee = false;
        visual = true;
    }

    protected void OnStart()
    {
        if (!IsOwner) return;

        //projectileSpawnPoint = GameObject.Find("ProjectileTransform").transform;
        canPrimaryFire = enablePrimaryFire;

        rangedWeapon.layer = LayerMask.NameToLayer("Hands");

        InitializeCooldowns();
    }

    protected virtual void InitializeCooldowns()
    {
        //secondary fire cooldown
        secondaryFireCDTime = secondaryFireCooldown;
    }

    protected void OnUpdate()
    {
        if(!IsOwner) return;

        base.Update();

        if(controller != null)
        {
            if (!controller.GetComponent<HealthManager>().IsAlive || !controller.Control) return;

            //handle cooldowns
            Cooldowns();

            //handle inputs
            if (Control)
                Inputs();

            //handle automated burst
            if (bursting && nextBurst)
                PrimaryFire();

            //handle visual resettings
            if (!melee)
            {
                if (primaryVisualResetting)
                {
                    if (primaryFireVisualResetTimer <= 0)
                        ResetPrimaryVisual();
                    else
                        primaryFireVisualResetTimer -= Time.deltaTime;
                }
            }
        }
        else
        {
            DestroyAllProjectiles();
        }
    }

    protected virtual void Cooldowns()
    {
        //handle cooldowns here

        //secondary fire
        if (secondaryFireCDTime != secondaryFireCooldown)
            secondaryFireCDTime = Mathf.Min(secondaryFireCooldown, secondaryFireCDTime + Time.deltaTime);
        else
            canSecondaryFire = true;
    }

    protected virtual void Inputs()
    {
        if (controller.statusEffects.stunned) return;

        if (Input.GetKeyDown(GameManager.bind_swapWeapon) || Input.GetAxis("Mouse ScrollWheel") != 0)
            SwapWeapon();

        if (!melee)
        {
            if (enablePrimaryFire && canPrimaryFire && Input.GetKey(GameManager.bind_primaryFire))
                StartBurst();
            else if (enableSecondaryFire && canSecondaryFire && Input.GetKey(GameManager.bind_secondaryFire))
                SecondaryFire();
        }
    }

    private void ResetPrimaryVisual()
    {
        primaryVisualResetting = false;
    }

    protected virtual void SwapWeapon()
    {
        if(bursting)
            EndBurst();
        melee = !melee;
    }

    protected virtual void SwapWeaponEnd()
    {
        canPrimaryFire = true;
    }

    #region Primary Fire

    private void PrimaryFire()
    {
        nextBurst = false;
        burstFiredCount++;
        FireProjectileAllLocal(ref canPrimaryFire, primarySpawnCameraOffset, primaryForwardForce, primaryUpForce, rangedResourceDirectory, primaryProjectile_Local[(burstFiredCount + 1) % 2], primaryProjectile_Network[(burstFiredCount + 1) % 2], projectileSpawnPoint[(burstFiredCount + 1) % 2], PrimaryOnImpact);

        if (burstFiredCount == burstCount)
        {
            EndBurst();
        }
        else
            Invoke(nameof(NextBurst), 1 / burstRate);
    }

    public void PrimaryOnImpact(GameObject context, Collider collider, RaycastHit hit)
    {
        if (collider.tag.Contains("Hitbox"))
        {
            if (collider.transform.root.tag != transform.root.tag && collider.transform.root.GetComponent<HealthManager>().IsAlive
                && collider.transform.root.gameObject != NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character)
            {
                //specific to this weapon, this allows it to pass through enemies without being destroyed
                if (!context.GetComponent<ThrowingAxe>().hits.Contains(collider.transform.root.gameObject))
                {
                    context.GetComponent<ThrowingAxe>().AddHit(collider.transform.root.gameObject);

                    //this stuff is neccessary though
                    //DestroyProjectile(context);
                    DealDamageToTarget(collider.transform.root.GetComponent<NetworkObject>().NetworkObjectId, collider.CompareTag("Hitbox_Critical") ? primaryDamage * 2 : primaryDamage);
                    //context.GetComponent<ProjectileImpact>().targetHit = true;
                }
            }
        }
        else
        {
            context.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            context.GetComponent<Rigidbody>().velocity = Vector3.zero;
            context.GetComponent<Collider>().enabled = false;
            context.transform.position = hit.point;
            foreach (Collider col in context.GetComponentsInChildren<Collider>())
                col.enabled = false;

            context.GetComponent<ProjectileImpact>().targetHit = true;
        }
    }

    private void ResetPrimaryFire()
    {
        canPrimaryFire = true;
        primaryFireVisualResetTimer = primaryFireVisualResetTime;
        primaryVisualResetting = true;
        burstFiredCount = 0;
    }

    private void StartBurst()
    {
        bursting = true;
        nextBurst = true;
        burstFiredCount = 0;
    }

    private void NextBurst()
    {
        nextBurst = true;
    }

    private void EndBurst()
    {
        bursting = false;
        nextBurst = false;
        Invoke(nameof(ResetPrimaryFire), primaryFireCooldown);
    }

    #endregion

    #region Secondary Fire

    private void SecondaryFire()
    {
        secondaryFireCDTime = 0f;

        GameObject instance = FireProjectileAllLocal(ref canSecondaryFire, secondarySpawnCameraOffset, secondaryForwardForce, secondaryUpForce, rangedResourceDirectory, secondaryProjectile_Local, secondaryProjectile_Network, projectileSpawnPoint[2], SecondaryOnImpact);

        OnSecondaryAttack.Invoke();
    }

    public void SecondaryOnImpact(GameObject context, Collider collider, RaycastHit hit)
    {
        if (collider.tag.Contains("Hitbox"))
        {
            if (collider.transform.root.tag != transform.root.tag && collider.transform.root.GetComponent<HealthManager>().IsAlive
                && collider.transform.root.gameObject != NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character)
            {
                //specific to this weapon, this allows it to pass through enemies without being destroyed
                if (!context.GetComponent<AxeThrowSecondary>().hits.Contains(collider.transform.root.gameObject))
                {
                    context.GetComponent<AxeThrowSecondary>().AddHit(collider.transform.root.gameObject);

                    //this stuff is neccessary though

                    DealDamageToTarget(collider.transform.root.GetComponent<NetworkObject>().NetworkObjectId, secondaryDamage);
                }
            }
        }
    }

    #endregion
}
