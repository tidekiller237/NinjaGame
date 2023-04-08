using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BasicWeapon : Weapon
{
    public bool melee;
    Animator animator;

    [Header("Projectile")]
    public GameObject rangedWeapon;
    public GameObject projectile_Network;
    public GameObject projectile_Local;
    public Transform projectileSpawnPoint;
    public LayerMask notPlayerMask;

    [Header("Primary Fire")]
    public bool enablePrimaryFire;
    public int primaryDamage;
    public float primarySpawnCameraOffset;
    public float primaryForwardForce;
    public float primaryUpForce;
    public float primaryFireCooldown;
    public float primaryFireVisualResetTime;
    bool canPrimaryFire;
    float primaryFireVisualResetTimer;
    bool primaryVisualResetting;

    [Header("Melee")]
    public bool enableMelee;
    public GameObject meleeWeapon;

    [Header("Melee Primary")]
    public bool enableMeleePrimary;
    public int meleePrimaryDamage;
    public float meleePrimarySwingDuration;
    public float meleePrimaryOffset;
    public Vector3 meleePrimaryBounds;
    public float meleePrimaryCooldown;
    bool meleeCheck;
    List<Collider> hitTargets = new List<Collider>();
    bool canMeleePrimary;

    protected override void Awake()
    {
        base.Awake();
        melee = false;
    }

    private void Start()
    {
        if (!IsOwner) return;

        projectileSpawnPoint = GameObject.Find("ProjectileTransform").transform;
        canPrimaryFire = enablePrimaryFire;
        canMeleePrimary = enableMeleePrimary;
        animator = GetComponentInParent<Animator>();
    }

    protected override void Update()
    {
        if (!IsOwner) return;

        base.Update();

        if (NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller != null)
        {
            animator.SetBool("Melee", melee);

            transform.position = GetComponentInParent<PlayerController>().mainCamera.transform.position;
            transform.rotation = GetComponentInParent<PlayerController>().mainCamera.transform.rotation;
            meleeWeapon.transform.position = transform.position + Vector3.up * -500f;
            rangedWeapon.transform.position = transform.position + Vector3.up * -500f;

            //handle inputs
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

    private void ResetAnimatorTriggers()
    {
        animator.ResetTrigger("MeleePrimary");
        animator.ResetTrigger("RangedPrimary");
        animator.ResetTrigger("RangedReset");
        animator.ResetTrigger("WeaponSwap");
    }

    private void GetInputs()
    {
        if (Input.GetKeyDown(GameManager.bind_swapWeapon))
        {
            melee = !melee;
            ResetAnimatorTriggers();
            animator.SetTrigger("WeaponSwap");
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
        }
    }

    private void DestroyAllProjectiles()
    {
        if (projDict.Count == 0) return;

        foreach (var obj in projDict)
        {
            if (obj.Value != null)
            {
                DestroyProjectileServerRPC(obj.Value.GetComponent<ProjectileBalistics>().netId);
                DestroyLocalProjectile(obj.Value);
            }
        }

        projDict.Clear();
    }

    private void PrimaryFire()
    {
        canPrimaryFire = false;
        CameraController cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * primarySpawnCameraOffset;
        Vector3 dir = cam.transform.forward;
        Vector3 force = dir * primaryForwardForce + transform.up * primaryUpForce;

        GameObject instance = PrimaryFireLocal(spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);
        projDict.Add(instance.GetInstanceID(), instance);
        PrimaryFireServerRPC(OwnerClientId, instance.GetInstanceID(), spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);

        Invoke(nameof(ResetPrimaryFire), primaryFireCooldown);

        ResetAnimatorTriggers();
        animator.SetTrigger("RangedPrimary");
    }

    private void ResetPrimaryFire()
    {
        canPrimaryFire = true;
        primaryFireVisualResetTimer = primaryFireVisualResetTime;
        primaryVisualResetting = true;
    }

    private void ResetPrimaryFireVisual()
    {
        ResetAnimatorTriggers();
        animator.SetTrigger("RangedReset");
        primaryVisualResetting = false;
    }

    public void PrimaryOnImpact(GameObject context, Collider collider)
    {
        if (collider.CompareTag("Player") && collider.GetComponent<HealthManager>().IsAlive
            && collider.gameObject != NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character)
        {
            UIManager.Instance.UI_HUD.GetComponent<UI_HUDManager>().DisplayHitmarker(0.15f);
            DestroyProjectileServerRPC(context.GetComponent<ProjectileBalistics>().netId);
            DestroyLocalProjectile(context);
        }
        else
        {
            context.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            context.GetComponent<Collider>().enabled = false;
        }

        if(collider.GetComponent<NetworkObject>() != null && collider.GetComponent<HealthManager>() != null)
            DealDamageToTargetServerRPC(OwnerClientId, collider.GetComponent<NetworkObject>().NetworkObjectId, primaryDamage);
    }

    #region Melee Weapon

    private void MeleePrimary()
    {
        canMeleePrimary = false;
        MeleeStart();
        Invoke(nameof(MeleeStop), meleePrimarySwingDuration);
        Invoke(nameof(ResetMeleePrimary), meleePrimaryCooldown);

        ResetAnimatorTriggers();
        animator.SetTrigger("MeleePrimary");
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
        Collider[] hits = MeleeCheck(controller.mainCamera.transform.position + controller.mainCamera.transform.forward * meleePrimaryOffset, meleePrimaryBounds);
        bool check = false;

        foreach (Collider collider in hits)
        {
            if (collider.GetComponent<NetworkObject>() != null && collider.GetComponent<HealthManager>() != null && !hitTargets.Contains(collider)
                && collider.gameObject != NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character)
            {
                check = true;
                DealDamageToTargetServerRPC(OwnerClientId, collider.GetComponent<NetworkObject>().NetworkObjectId, meleePrimaryDamage);
                hitTargets.Add(collider);
            }
        }

        if(check)
            UIManager.Instance.UI_HUD.GetComponent<UI_HUDManager>().DisplayHitmarker(0.15f);
    }

    private Collider[] MeleeCheck(Vector3 position, Vector3 bounds)
    {
        Collider[] cols = Physics.OverlapBox(position, bounds / 2f);
        List<Collider> result = new List<Collider>();

        foreach (Collider col in cols)
        {
            if (col.CompareTag("Player") && col.GetComponent<HealthManager>().IsAlive)
                result.Add(col);
        }

        return result.ToArray();
    }

    #endregion

    #region Network

    private GameObject PrimaryFireLocal(Vector3 spawn, Vector3 visualSpawn, Quaternion orientation, Vector3 force)
    {
        GameObject instance = Instantiate(projectile_Local, spawn, orientation);
        Rigidbody instantceRb = instance.GetComponent<Rigidbody>();
        instance.GetComponent<ProjectileBalistics>().SetSpawnPosition(visualSpawn);
        instance.GetComponent<ProjectileInpact>().OnImpact.AddListener(PrimaryOnImpact);
        instantceRb.AddForce(force, ForceMode.Impulse);
        return instance;
    }

    [ServerRpc]
    private void PrimaryFireServerRPC(ulong clientId, int localId, Vector3 spawn, Vector3 visualSpawn, Quaternion orientation, Vector3 force)
    {
        GameObject netInstance = Instantiate(projectile_Network, spawn, orientation);
        Rigidbody instantceRb = netInstance.GetComponent<Rigidbody>();
        netInstance.GetComponent<NetworkObject>().Spawn();
        instantceRb.AddForce(force, ForceMode.Impulse);
        netInstance.GetComponent<ProjectileBalistics>().SetSpawnPosition(visualSpawn);
        DisableNetworkProjectileClientRPC(clientId, netInstance.GetComponent<NetworkObject>().NetworkObjectId, localId);
    }

    [ServerRpc]
    private void DestroyProjectileServerRPC(ulong objectId)
    {
        GetNetworkObject(objectId).Despawn();
    }

    [ServerRpc]
    private void DealDamageToTargetServerRPC(ulong client, ulong objectId, int damage)
    {
        NetworkObject target = GetNetworkObject(objectId);
        if(target.GetComponent<HealthManager>() != null)
        {
            target.GetComponent<HealthManager>().Damage(damage);
        }
    }

    [ClientRpc]
    private void DisableNetworkProjectileClientRPC(ulong clientId, ulong netObjectId, int localId)
    {
        if(IsOwner && OwnerClientId == clientId)
        {
            NetworkObject netObject = GetNetworkObject(netObjectId);
            foreach (Transform child in netObject.transform)
                child.gameObject.SetActive(false);
            netObject.GetComponent<Collider>().enabled = false;
            GameObject localInstance;
            projDict.TryGetValue(localId, out localInstance);

            if (localInstance == null) return;
            localInstance.GetComponent<ProjectileBalistics>().netId = netObjectId;
        }
    }

    #endregion
}
