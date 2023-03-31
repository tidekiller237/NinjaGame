using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BasicWeapon : Weapon
{
    [Header("Projectile")]
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
    bool canPrimaryFire;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (!IsOwner) return;

        projectileSpawnPoint = GameObject.Find("ProjectileTransform").transform;
        canPrimaryFire = enablePrimaryFire;
    }

    protected override void Update()
    {
        if (!IsOwner) return;

        base.Update();

        if (PlayerController.instance.control)
        {
            //handle inputs
            GetInputs();
        }
        else
        {
            DestroyAllProjectiles();
        }
    }

    private void GetInputs()
    {
        if(enablePrimaryFire && canPrimaryFire && Input.GetKey(primaryInput))
        {
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
        CameraController cam = PlayerController.instance.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * primarySpawnCameraOffset;
        Vector3 dir = cam.transform.forward;
        Vector3 force = dir * primaryForwardForce + transform.up * primaryUpForce;

        GameObject instance = PrimaryFireLocal(spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);
        projDict.Add(instance.GetInstanceID(), instance);
        PrimaryFireServerRPC(OwnerClientId, instance.GetInstanceID(), spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);

        Invoke(nameof(ResetPrimaryFire), primaryFireCooldown);
    }

    private void ResetPrimaryFire()
    {
        canPrimaryFire = true;
    }

    public void PrimaryOnImpact(GameObject context, Collider collider)
    {
        if (collider.CompareTag("Player") && collider.GetComponent<HealthManager>().IsAlive)
        {
            UIManager.Instance.UI_HUD.GetComponent<UI_HUDManager>().DisplayHitmarker(0.25f);
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
