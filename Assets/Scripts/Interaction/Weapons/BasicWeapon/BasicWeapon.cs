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

        //handle inputs
        GetInputs();
    }

    private void GetInputs()
    {
        if(canPrimaryFire && Input.GetKey(primaryInput))
        {
            PrimaryFire();
        }
    }

    private void PrimaryFire()
    {
        canPrimaryFire = false;
        CameraController cam = PlayerController.instance.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * primarySpawnCameraOffset;
        Vector3 dir = cam.transform.forward;
        Vector3 force = dir * primaryForwardForce + transform.up * primaryUpForce;

        PrimaryFireServerRPC(OwnerClientId, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);
        PrimaryFireLocal(spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);

        Invoke(nameof(ResetPrimaryFire), primaryFireCooldown);
    }

    private void ResetPrimaryFire()
    {
        canPrimaryFire = true;
    }

    public void PrimaryOnImpact(GameObject context, Collider collider)
    {
        if (context.GetComponent<NetworkObject>() != null && IsServer)
        {
            if (collider.CompareTag("Player"))
            {
                if(IsOwner)
                UIManager.Instance.UI_HUD.GetComponent<UI_HUDManager>().DisplayHitmarker(0.25f);

                context.GetComponent<NetworkObject>().Despawn();
            }
            else
            {
                //ContactPoint contactPoint = collision.GetContact(0);
                //context.transform.position = contactPoint.point;
                context.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                context.GetComponent<Collider>().enabled = false;
            }
        }
        else
        {
            if (collider.CompareTag("Player"))
            {
                //damage player
                UIManager.Instance.UI_HUD.GetComponent<UI_HUDManager>().DisplayHitmarker(0.25f);
                Destroy(context);
            }
            else
            {
                //ContactPoint contactPoint = collision.GetContact(0);
                //context.transform.position = contactPoint.point;
                context.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                context.GetComponent<Collider>().enabled = false;
            }
        }
    }

    #region Network

    [ServerRpc]
    private void PrimaryFireServerRPC(ulong clientId, Vector3 spawn, Vector3 visualSpawn, Quaternion orientation, Vector3 force)
    {
        GameObject netInstance = Instantiate(projectile_Network, spawn, orientation);
        Rigidbody instantceRb = netInstance.GetComponent<Rigidbody>();
        netInstance.GetComponent<NetworkObject>().Spawn();
        instantceRb.AddForce(force, ForceMode.Impulse);
        netInstance.GetComponent<ProjectileBalistics>().SetSpawnPosition(visualSpawn);
        DisableNetworkProjectileClientRPC(clientId, netInstance.GetComponent<NetworkObject>().NetworkObjectId);
        netInstance.GetComponent<ProjectileInpact>().OnImpact.AddListener(PrimaryOnImpact);
    }

    private void PrimaryFireLocal(Vector3 spawn, Vector3 visualSpawn, Quaternion orientation, Vector3 force)
    {
        if (IsServer) return;
        GameObject instance = Instantiate(projectile_Local, spawn, orientation);
        Rigidbody instantceRb = instance.GetComponent<Rigidbody>();
        instantceRb.AddForce(force, ForceMode.Impulse);
        instance.GetComponent<ProjectileBalistics>().SetSpawnPosition(visualSpawn);
        instance.GetComponent<ProjectileInpact>().OnImpact.AddListener(PrimaryOnImpact);
    }

    [ClientRpc]
    private void DisableNetworkProjectileClientRPC(ulong clientId, ulong netObjectId)
    {
        if(IsOwner && !IsServer && OwnerClientId == clientId)
        {
            NetworkObject netObject = GetNetworkObject(netObjectId);
            foreach (Transform child in netObject.transform)
                child.gameObject.SetActive(false);
            netObject.GetComponent<Collider>().enabled = false;
        }
    }

    #endregion
}
