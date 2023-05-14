using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using Unity.Collections;

public class Weapon : NetworkBehaviour
{
    public bool Activated { private get; set; }
    public PlayerController controller { protected get; set; }
    public bool Control { get; private set; }
    bool tempControl;

    protected Dictionary<int, GameObject> projDict;
    public bool melee;
    public UnityEvent OnAttack = new UnityEvent();
    public UnityEvent OnAttackEnd = new UnityEvent();
    public UnityEvent OnSecondaryAttack = new UnityEvent();
    public UnityEvent OnSecondaryAttackEnd = new UnityEvent();
    public LayerMask meleeBlockLayers;

    //weapon variables
    protected bool visual;

    [Header("Flags")]
    public bool enablePrimaryFire;
    public bool enableSecondaryFire;
    public bool enableMeleePrimary;
    public bool enableMeleeSecondary;

    [Header("Visual")]
    public GameObject firstPersonWeaponParent;

    protected virtual void Awake()
    {
        Activated = false;
        projDict = new Dictionary<int, GameObject>();
        Control = true;
    }

    protected virtual void Update()
    {
        enabled = Activated;
    }

    public void SetVisual(bool value)
    {
        visual = value;
    }

    public void SetControl(bool value, float delay = 0f)
    {
        if (delay <= 0f)
            Control = value;
        else
        {
            tempControl = value;
            Invoke(nameof(SetControlDelayed), delay);
        }
    }

    private void SetControlDelayed()
    {
        Control = tempControl;
    }

    #region Ranged Weapon

    protected GameObject FireProjectile(ref bool canFire, float spawnCameraOffset, float forwardForce, float upForce, string projectileDirectory, string projectile_Local, string projectile_Network, Transform projectileSpawnPoint, UnityAction<GameObject, Collider, RaycastHit> onImpactCallback)
    {
        canFire = false;
        CameraController cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * spawnCameraOffset;
        Vector3 dir = cam.transform.forward;
        Vector3 force = dir * forwardForce + transform.up * upForce;

        GameObject instance = FireProjectileLocal(projectileDirectory, projectile_Local, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force, onImpactCallback);
        projDict.Add(instance.GetInstanceID(), instance);
        FireProjectileNetwork(instance.GetInstanceID(), projectileDirectory, projectile_Network, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);

        OnAttack.Invoke();
        return instance;
    }

    protected GameObject FireProjectile(ref bool canFire, float spawnCameraOffset, Vector3 force, string projectileDirectory, string projectile_Local, string projectile_Network, Transform projectileSpawnPoint, UnityAction<GameObject, Collider, RaycastHit> onImpactCallback)
    {
        canFire = false;
        CameraController cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * spawnCameraOffset;

        GameObject instance = FireProjectileLocal(projectileDirectory, projectile_Local, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force, onImpactCallback);
        projDict.Add(instance.GetInstanceID(), instance);
        FireProjectileNetwork(instance.GetInstanceID(), projectileDirectory, projectile_Network, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force);

        OnAttack.Invoke();
        return instance;
    }

    protected GameObject FireProjectileAllLocal(ref bool canFire, float spawnCameraOffset, float forwardForce, float upForce, string projectileDirectory, string projectile_Local, string projectile_Network, Transform projectileSpawnPoint, UnityAction<GameObject, Collider, RaycastHit> onImpactCallback)
    {
        canFire = false;
        CameraController cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * spawnCameraOffset;
        Vector3 dir = cam.transform.forward;
        Vector3 force = dir * forwardForce + transform.up * upForce;

        GameObject instance = FireProjectileLocal(projectileDirectory, projectile_Local, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force, onImpactCallback);
        projDict.Add(instance.GetInstanceID(), instance);
        FireProjectileLocalServerRpc(OwnerClientId, projectileDirectory, projectile_Network, spawnLocation, instance.transform.rotation, force);

        OnAttack.Invoke();
        return instance;
    }

    protected GameObject FireProjectileAllLocal(ref bool canFire, float spawnCameraOffset, float forwardForce, float upForce, string projectileDirectory, string projectile_Local, string projectile_Network, Transform projectileSpawnPoint, Quaternion projectileOrientation, UnityAction<GameObject, Collider, RaycastHit> onImpactCallback)
    {
        canFire = false;
        CameraController cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * spawnCameraOffset;
        Vector3 dir = cam.transform.forward;
        Vector3 force = dir * forwardForce + transform.up * upForce;

        GameObject instance = FireProjectileLocal(projectileDirectory, projectile_Local, spawnLocation, projectileSpawnPoint.position, projectileOrientation, force, onImpactCallback);
        projDict.Add(instance.GetInstanceID(), instance);
        FireProjectileLocalServerRpc(OwnerClientId, projectileDirectory, projectile_Network, spawnLocation, instance.transform.rotation, force);

        OnAttack.Invoke();
        return instance;
    }

    protected void FireProjectileAllLocal(ref bool canFire, float spawnCameraOffset, Vector3 force, string projectileDirectory, string projectile_Local, string projectile_Network, Transform projectileSpawnPoint, UnityAction<GameObject, Collider, RaycastHit> onImpactCallback)
    {
        canFire = false;
        CameraController cam = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller.mainCamera;
        Vector3 spawnLocation = cam.transform.position + cam.transform.forward * spawnCameraOffset;

        GameObject instance = FireProjectileLocal(projectileDirectory, projectile_Local, spawnLocation, projectileSpawnPoint.position, cam.transform.rotation, force, onImpactCallback);
        projDict.Add(instance.GetInstanceID(), instance);
        FireProjectileLocalServerRpc(OwnerClientId, projectileDirectory, projectile_Network, spawnLocation, instance.transform.rotation, force);

        OnAttack.Invoke();
    }

    [ServerRpc]
    private void FireProjectileLocalServerRpc(ulong clientId, FixedString64Bytes prefabDirectory, FixedString64Bytes prefab, Vector3 spawn, Quaternion orientation, Vector3 force)
    {
        FireProjectileLocalClientRpc(clientId, prefabDirectory, prefab, spawn, orientation, force);
    }

    [ClientRpc]
    private void FireProjectileLocalClientRpc(ulong clientId, FixedString64Bytes prefabDirectory, FixedString64Bytes prefab, Vector3 spawn, Quaternion orientation, Vector3 force)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId) return;

        GameObject instance = Instantiate(Database.LoadProjectile(prefabDirectory.ToString(), prefab.ToString()), spawn, orientation);
        Rigidbody instanceRb = instance.GetComponent<Rigidbody>();
        instanceRb.AddForce(force, ForceMode.Impulse);
    }

    #endregion

    #region Projectiles

    protected GameObject FireProjectileLocal(string prefabDirectory, string prefabName, Vector3 spawn, Vector3 visualSpawn, Quaternion orientation, Vector3 force, UnityAction<GameObject, Collider, RaycastHit> callback)
    {
        GameObject instance = Instantiate(Database.LoadProjectile(prefabDirectory, prefabName), spawn, orientation);
        Rigidbody instantceRb = instance.GetComponent<Rigidbody>();
        instance.GetComponent<ProjectileBalistics>().SetSpawnPosition(visualSpawn);
        instance.GetComponent<ProjectileImpact>().OnImpact.AddListener(callback);

        if (instance.GetComponent<ProjectileCleanup>())
            instance.GetComponent<ProjectileCleanup>().weapon = this;
        
        instantceRb.AddForce(force, ForceMode.Impulse);
        return instance;
    }

    protected void FireProjectileNetwork(int localId, string prefabDirectory, string prefab, Vector3 spawn, Vector3 visualSpawn, Quaternion orientation, Vector3 force)
    {
        FireProjectileServerRPC(OwnerClientId, localId, new FixedString64Bytes(prefabDirectory), new FixedString64Bytes(prefab), spawn, visualSpawn, orientation, force);
    }

    [ServerRpc]
    private void FireProjectileServerRPC(ulong clientId, int localId, FixedString64Bytes prefabDirectory, FixedString64Bytes prefab, Vector3 spawn, Vector3 visualSpawn, Quaternion orientation, Vector3 force)
    {
        GameObject netInstance = Instantiate(Database.LoadProjectile(prefabDirectory.ToString(), prefab.ToString()), spawn, orientation);
        Rigidbody instantceRb = netInstance.GetComponent<Rigidbody>();
        netInstance.GetComponent<NetworkObject>().Spawn();
        instantceRb.AddForce(force, ForceMode.Impulse);
        netInstance.GetComponent<ProjectileBalistics>().SetSpawnPosition(visualSpawn);
        DisableNetworkProjectileClientRPC(clientId, netInstance.GetComponent<NetworkObject>().NetworkObjectId, localId);
    }

    [ClientRpc]
    private void DisableNetworkProjectileClientRPC(ulong clientId, ulong netObjectId, int localId)
    {
        if (IsOwner && OwnerClientId == clientId)
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

    protected void DestroyAllProjectiles()
    {
        if (!IsOwner || projDict.Count == 0) return;

        foreach (var obj in projDict)
        {
            if (obj.Value != null)
            {
                DestroyProjectile(obj.Value);
            }
        }

        projDict.Clear();
    }

    public void DestroyProjectile(GameObject projectile)
    {
        if (!IsOwner) return;

        if (projDict.ContainsKey(projectile.GetInstanceID()))
        {
            projectile.SetActive(false);
            StartCoroutine(DestroyNetProjectile(projectile.GetComponent<ProjectileBalistics>(), 5f));
        }
        else
            Destroy(projectile);
    }

    protected IEnumerator DestroyNetProjectile(ProjectileBalistics context, float tryTime)
    {
        float time = 0;
        bool check = false;

        while (time <= tryTime)
        {
            if (context.netId != 0)
            {
                projDict.Remove(context.GetInstanceID());
                Destroy(context.gameObject);
                DestroyProjectileServerRPC(context.netId);
                check = true;
                break;
            }

            time += Time.deltaTime;
            yield return null;
        }

        if (!check)
        {
            projDict.Remove(context.GetInstanceID());
            Destroy(context);
        }
    }

    [ServerRpc]
    protected void DestroyProjectileServerRPC(ulong objectId)
    {
        NetworkObject obj = GetNetworkObject(objectId);
        obj.Despawn();
    }

    protected void SpawnParticleSystemLocal(string prefabDirectory, string prefabName, Vector3 position)
    {
        Instantiate(Database.LoadParticleSystem(prefabDirectory, prefabName)).transform.position = position;
    }

    protected void SpawnParticleSystemNetwork(string prefabDirectory, string prefabName, Vector3 position)
    {
        SpawnProjectileServerRpc(OwnerClientId, new FixedString64Bytes(prefabDirectory), new FixedString64Bytes(prefabName), position);
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(ulong clientId, FixedString64Bytes prefabDirectory, FixedString64Bytes prefabName, Vector3 position)
    {
        SpawnProjectileClientRpc(prefabDirectory, prefabName, position);
    }

    [ClientRpc]
    private void SpawnProjectileClientRpc(FixedString64Bytes prefabDirectory, FixedString64Bytes prefabName, Vector3 position)
    {
        if (IsOwner) return;

        Instantiate(Database.LoadParticleSystem(prefabDirectory.ToString(), prefabName.ToString())).transform.position = position;
    }

    #endregion

    #region Melee

    /// <summary>
    /// Returns the roots of all colliders hit
    /// </summary>
    protected GameObject[] MeleeCheck(Transform origin, float distance, float radius)
    {
        Collider[] cols = Physics.OverlapCapsule(origin.position, origin.position + origin.forward * distance, radius);
        List<GameObject> result = new List<GameObject>();

        foreach (Collider col in cols)
        {
            bool losCheck = !Physics.Raycast(origin.position, col.transform.position - origin.position, Vector3.Distance(origin.position, col.transform.position), meleeBlockLayers);
            if (col.tag.Contains("Hitbox") && col.transform.root.GetComponent<HealthManager>().IsAlive && !result.Contains(col.transform.root.gameObject) && losCheck)
                result.Add(col.transform.root.gameObject);
        }

        return result.ToArray();
    }

    #endregion

    #region Damage

    public void DealDamageToTarget(ulong objectId, int damage)
    {
        UIManager.Instance.UI_HUD.GetComponent<UI_HUDManager>().DisplayHitmarker(0.15f);
        DealDamageToTargetServerRPC(OwnerClientId, objectId, damage);
    }

    [ServerRpc]
    private void DealDamageToTargetServerRPC(ulong client, ulong objectId, int damage)
    {
        NetworkObject target = GetNetworkObject(objectId);
        if (target.GetComponent<HealthManager>() != null)
        {
            target.GetComponent<HealthManager>().Damage(damage);
        }
    }

    #endregion
}
