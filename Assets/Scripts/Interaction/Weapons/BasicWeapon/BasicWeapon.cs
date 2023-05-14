using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class BasicWeapon : Weapon
{
    [Header("Projectile")]
    public GameObject rangedWeapon;
    public string primaryProjectileDirectory;
    public string primaryProjectile_Network;
    public string primaryProjectile_Local;
    public string secondaryProjectileDirectory;
    public string secondaryProjectile_Network;
    public string secondaryProjectile_Local;
    public string secondaryExplosionDirectory;
    public string secondaryExplosionPrefab;
    public Transform projectileSpawnPoint;
    public LayerMask notPlayerMask;

    [Header("Primary Fire")]
    //public bool enablePrimaryFire;
    public int primaryDamage;
    public float primarySpawnCameraOffset;
    public float primaryForwardForce;
    public float primaryUpForce;
    public float primaryFireCooldown;
    public float primaryFireVisualResetTime;
    bool canPrimaryFire;
    float primaryFireVisualResetTimer;
    bool primaryVisualResetting;

    [Header("Secondary Fire")]
    //public bool enableSecondaryFire;
    public int secondaryExplosionDamage;
    public float secondaryExplosionRadius;
    public float secondaryExplosionTriggerTime;
    public float secondaryFireCooldown;
    public float secondaryCooldownTime;
    bool canSecondaryFire;

    [Header("Melee")]
    public bool enableMelee;
    public GameObject meleeWeapon;

    [Header("Melee Primary")]
    //public bool enableMeleePrimary;
    public int meleePrimaryDamage;
    public float meleePrimarySwingDuration;
    public float meleePrimaryDistance;
    public float meleePrimaryRadius;
    public float meleePrimaryCooldown;
    bool meleeCheck;
    List<GameObject> hitTargets = new List<GameObject>();
    bool canMeleePrimary;

    [Header("Weapon Swap")]
    public float swapDelay;

    protected override void Awake()
    {
        base.Awake();
        melee = false;
        visual = true;
    }

    private void Start()
    {
        if (!IsOwner) return;

        projectileSpawnPoint = GameObject.Find("ProjectileTransform").transform;
        canPrimaryFire = enablePrimaryFire;
        canSecondaryFire = enableSecondaryFire;
        canMeleePrimary = enableMeleePrimary;

        rangedWeapon.layer = LayerMask.NameToLayer("Hands");
        meleeWeapon.layer = LayerMask.NameToLayer("Hands");
    }

    protected override void Update()
    {
        if (!IsOwner) return;

        base.Update();
        firstPersonWeaponParent.SetActive(visual);

        if (NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller != null && controller != null)
        {
            if (!controller.GetComponent<HealthManager>().IsAlive || !controller.Control) return;

            //transform.position = GetComponentInParent<PlayerController>().mainCamera.transform.position;
            //transform.rotation = GetComponentInParent<PlayerController>().mainCamera.transform.rotation;
            //meleeWeapon.transform.position = transform.position + Vector3.up * -500f;
            //rangedWeapon.transform.position = transform.position + Vector3.up * -500f;

            //handle cooldowns
            HandleCooldowns();

            //handle inputs
            if(Control)
                GetInputs();

            if (melee)
            {
                if (meleeCheck)
                    MeleePrimaryImpactCheck();
            }
            else
            {

                //reset primary fire visual
                if (primaryVisualResetting)
                {
                    if (primaryFireVisualResetTimer <= 0)
                        ResetPrimaryFireVisual();
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

    private void GetInputs()
    {
        if (Input.GetKeyDown(GameManager.bind_swapWeapon) || Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            SwapWeapon();
        }

        if (melee)
        {
            if (enableMeleePrimary && canMeleePrimary && Input.GetKey(GameManager.bind_primaryFire))
                MeleePrimary();
        }
        else
        {
            if (enablePrimaryFire && canPrimaryFire && Input.GetKey(GameManager.bind_primaryFire))
                PrimaryFire();
            else if (enableSecondaryFire && canSecondaryFire && Input.GetKey(GameManager.bind_secondaryFire))
                SecondaryFire();
        }
    }

    private void HandleCooldowns()
    {
        //secondary fire cooldown
        if (secondaryCooldownTime < secondaryFireCooldown)
        {
            canSecondaryFire = false;
            secondaryCooldownTime += Time.deltaTime;
            secondaryCooldownTime = Mathf.Min(secondaryCooldownTime, secondaryFireCooldown);
        }
        else
            canSecondaryFire = true;
    }

    private void SwapWeapon()
    {
        melee = !melee;
        canMeleePrimary = false;
        canPrimaryFire = false;
        canSecondaryFire = false;

        CancelInvoke();
        Invoke(nameof(SwapWeaponEnd), (melee) ? meleePrimaryCooldown : primaryFireCooldown);
    }

    private void SwapWeaponEnd()
    {
        canMeleePrimary = true;
        canPrimaryFire = true;
        canSecondaryFire = true;
    }

    #region Ranged Weapon

    // PRIMARY FIRE

    private void PrimaryFire()
    {
        canPrimaryFire = false;
        CameraController cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * primarySpawnCameraOffset;
        Vector3 dir = cam.transform.forward;
        Vector3 force = dir * primaryForwardForce + transform.up * primaryUpForce;

        GameObject instance = FireProjectileLocal(primaryProjectileDirectory, primaryProjectile_Local, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force, PrimaryOnImpact);
        projDict.Add(instance.GetInstanceID(), instance);
        FireProjectileNetwork(instance.GetInstanceID(), primaryProjectileDirectory, primaryProjectile_Network, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);

        Invoke(nameof(ResetPrimaryFire), primaryFireCooldown);

        OnAttack.Invoke();
    }

    private void ResetPrimaryFire()
    {
        canPrimaryFire = true;
        primaryFireVisualResetTimer = primaryFireVisualResetTime;
        primaryVisualResetting = true;
    }

    private void ResetPrimaryFireVisual()
    {
        primaryVisualResetting = false;
    }

    public void PrimaryOnImpact(GameObject context, Collider collider, RaycastHit hit)
    {
        if (collider.tag.Contains("Hitbox"))
        {
            if (collider.transform.root.tag != transform.root.tag && collider.transform.root.GetComponent<HealthManager>().IsAlive
                && collider.transform.root.gameObject != NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character)
            {
                DestroyProjectile(context);
                DealDamageToTarget(collider.transform.root.GetComponent<NetworkObject>().NetworkObjectId, collider.CompareTag("Hitbox_Critical") ? primaryDamage * 2 : primaryDamage);
                context.GetComponent<ProjectileImpact>().targetHit = true;
            }
        }
        else
        {
            context.GetComponent<ProjectileImpact>().targetHit = true;
        }
    }

    // SECONDARY FIRE

    private void SecondaryFire()
    {
        secondaryCooldownTime = 0f;

        CameraController cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * primarySpawnCameraOffset;
        Vector3 dir = cam.transform.forward;
        Vector3 force = dir * primaryForwardForce + transform.up * primaryUpForce;

        GameObject instance = FireProjectileLocal(secondaryProjectileDirectory, secondaryProjectile_Local, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force, SecondaryOnImpact);
        projDict.Add(instance.GetInstanceID(), instance);
        FireProjectileNetwork(instance.GetInstanceID(), secondaryProjectileDirectory, secondaryProjectile_Network, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);

        OnAttack.Invoke();
    }

    public void SecondaryOnImpact(GameObject context, Collider collider, RaycastHit hit)
    {
        if (collider.tag.Contains("Hitbox"))
        {
            if (collider.transform.root.tag != transform.root.tag && collider.transform.root.GetComponent<HealthManager>().IsAlive
                && collider.transform.root.gameObject != NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character)
            {
                //UIManager.Instance.UI_HUD.GetComponent<UI_HUDManager>().DisplayHitmarker(0.15f);
                //DestroyProjectile(context);
                DealDamageToTarget(collider.transform.root.GetComponent<NetworkObject>().NetworkObjectId, collider.CompareTag("Hitbox_Critical") ? primaryDamage * 2 : primaryDamage);
                context.GetComponent<ProjectileImpact>().targetHit = true;

                //trigger effect if hit player
                SecondaryOnTrigger(context);
            }
        }
        else
        {
            //context.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            //context.GetComponent<Rigidbody>().velocity = Vector3.zero;
            //context.GetComponent<Collider>().enabled = false;
            //context.GetComponent<ProjectileImpact>().targetHit = true;

            //set effect to trigger when hitting anything else
            //context.transform.position = hitPoint - context.transform.forward * 0.1f;
            context.GetComponent<ExplosiveKunai>().OnTrigger.AddListener(SecondaryOnTrigger);
            context.GetComponent<ExplosiveKunai>().SetTrigger(secondaryExplosionTriggerTime);
        }
    }

    public void SecondaryOnTrigger(GameObject context)
    {
        Vector3 explosionOrigin = context.transform.position - context.transform.forward * 0.1f;

        //trigger visual
        SpawnParticleSystemLocal(secondaryExplosionDirectory, secondaryExplosionPrefab, explosionOrigin);
        SpawnParticleSystemNetwork(secondaryExplosionDirectory, secondaryExplosionPrefab, explosionOrigin);

        //process effect
        Collider[] colliders = Physics.OverlapSphere(explosionOrigin, secondaryExplosionRadius);
        List<GameObject> hitList = new List<GameObject>();

        foreach(Collider col in colliders)
        {
            if(col.tag.Contains("Hitbox") && !hitList.Contains(col.transform.root.gameObject) && !col.CompareTag(transform.root.tag)
                && col.transform.root.GetComponent<HealthManager>().IsAlive)
            {
                if (!Physics.Raycast(explosionOrigin, col.transform.root.position, Vector3.Distance(explosionOrigin, col.transform.root.position), notPlayerMask))
                {
                    GameObject root = col.transform.root.gameObject;
                    DealDamageToTarget(root.GetComponent<NetworkObject>().NetworkObjectId, secondaryExplosionDamage);
                    hitList.Add(root);
                }
            }
        }

        DestroyProjectile(context);
    }

    #endregion

    #region Melee Weapon

    private void MeleePrimary()
    {
        canMeleePrimary = false;
        MeleeStart();
        Invoke(nameof(MeleeStop), meleePrimarySwingDuration);
        Invoke(nameof(ResetMeleePrimary), meleePrimaryCooldown);

        OnAttack.Invoke();
    }

    private void ResetMeleePrimary()
    {
        canMeleePrimary = true;
    }

    public void MeleeStart()
    {
        meleeCheck = true;
        hitTargets.Clear();
    }

    public void MeleeStop()
    {
        meleeCheck = false;
    }

    private void MeleePrimaryImpactCheck()
    {
        PlayerController controller = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller;
        GameObject[] hits = MeleeCheck(controller.mainCamera.transform, meleePrimaryDistance, meleePrimaryRadius);
        bool check = false;

        foreach (GameObject obj in hits)
        {
            if (!hitTargets.Contains(obj) && obj.gameObject != NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character && obj.tag != transform.root.tag)
            {
                check = true;
                DealDamageToTarget(obj.transform.root.GetComponent<NetworkObject>().NetworkObjectId, meleePrimaryDamage);
                hitTargets.Add(obj);
            }
        }
    }

    #endregion
}
