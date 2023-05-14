using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Berserker : Ability
{
    Barbarian playerClass;

    [Header("Rage")]
    public float rageDuration;

    [Header("Ability 1")]
    public int ab1Damage;
    public float ab1Radius;
    public float ab1Duration;
    public float ab1UpDistance;
    public float ab1UpSpeed;

    [Header("Ability 2")]
    public float ab2MinHeight;
    public int ab2Damage;
    public float ab2Radius;
    public float ab2Speed;
    public float ab2FloatTime;
    [HideInInspector] public bool ab2HeightCheck;
    bool ab2Active;

    public UnityEvent onRage = new UnityEvent();
    public UnityEvent onAbility1 = new UnityEvent();
    public UnityEvent onAbility2 = new UnityEvent();

    protected override void Start()
    {
        if (!IsOwner) return;

        base.Start();
        playerClass = PlayerClass.GetComponent<Barbarian>();
    }

    protected override void Update()
    {
        if (!IsOwner) return;
        base.Update();

        Cooldowns();

        if (PlayerWeapon.Control)
            Inputs();

        if(playerClass.CheckMaxCost())
            classAbilityCDTime = 1f;
        else
            classAbilityCDTime = 0f;

        if (ab2Active && Controller.Grounded)
            StopAbility2();

        ab2HeightCheck = !Physics.Raycast(Controller.transform.position, Vector3.down, ab2MinHeight + (Controller.GetComponent<CapsuleCollider>().height / 2), GameManager.Instance.GroundMask);
    }

    private void Inputs()
    {
        if (Controller.statusEffects.stunned) return;

        if (enableAbility1 && canAbility1 && Input.GetKey(GameManager.bind_ability1))
            Uppercut();

        if (enableAbility2 && canAbility2 && Input.GetKey(GameManager.bind_ability2))
            GroundSlam();

        if (enableClassAbility && playerClass.CheckMaxCost() && Input.GetKey(GameManager.bind_ability3))
            ActivateRage();
    }

    private void Cooldowns()
    {
        if (ability1CDTime < ability1Cooldown)
        {
            ability1CDTime = Mathf.Min(ability1CDTime + Time.deltaTime, ability1Cooldown);
            canAbility1 = false;
        }
        else
            canAbility1 = true;

        if (ability2CDTime < ability2Cooldown)
        {
            ability2CDTime = Mathf.Min(ability2CDTime + Time.deltaTime, ability2Cooldown);
            canAbility2 = false;
        }
        else
            canAbility2 = true;

        if (ability3CDTime < ability3Cooldown)
        {
            ability3CDTime = Mathf.Min(ability3CDTime + Time.deltaTime, ability3Cooldown);
            canAbility3 = false;
        }
        else
            canAbility3 = true;
    }

    #region Abilities

    private void ActivateRage()
    {
        onRage.Invoke();
    }

    private void Uppercut()
    {
        if (Controller.statusEffects.rooted || Controller.statusEffects.hovered) return;

        hitTargets.Clear();
        Vector3 origin = Controller.transform.position;
        List<ulong> hitIds = new List<ulong>();

        Collider[] cols = Physics.OverlapSphere(origin, ab1Radius);

        foreach (Collider col in cols)
        {
            Transform root = col.transform.root;
            bool losCheck = !Physics.Raycast(origin, root.position - origin, Vector3.Distance(origin, root.position), GameManager.Instance.GroundMask);
            if (col.tag.Contains("Hitbox") && root.GetComponent<HealthManager>().IsAlive && !hitTargets.Contains(root.gameObject) && losCheck && !Controller.CompareTag(root.tag))
            {
                ulong hitId = root.GetComponent<NetworkObject>().NetworkObjectId;
                hitIds.Add(root.GetComponent<NetworkObject>().OwnerClientId);
                PlayerWeapon.DealDamageToTarget(hitId, ab1Damage);
                hitTargets.Add(root.gameObject);
            }
        }

        //apply effects to enemies
        if(hitIds.Count > 0)
            UpperCutServerRpc(OwnerClientId, hitIds.ToArray(), ab1Duration, ab1UpDistance, ab1UpSpeed);

        //apply effects to player
        Controller.statusEffects.Hover(ab1Duration);
        StartCoroutine(UpperCutMove(Controller, ab1UpDistance, ab1UpSpeed));
        Invoke(nameof(StopAbility1), ab1Duration);

        onAbility1.Invoke();
    }

    private void StopAbility1()
    {
        ability1CDTime = 0f;
    }

    private void GroundSlam()
    {
        if (Controller.statusEffects.rooted || Controller.statusEffects.hovered || !ab2HeightCheck) return;

        ab2Active = true;

        hitTargets.Clear();
        Vector3 origin = Controller.transform.position;
        List<ulong> hitIds = new List<ulong>();

        Collider[] cols = Physics.OverlapSphere(origin, ab2Radius);

        foreach (Collider col in cols)
        {
            Transform root = col.transform.root;
            bool losCheck = !Physics.Raycast(origin, root.position - origin, Vector3.Distance(origin, root.position), GameManager.Instance.GroundMask);
            if (col.tag.Contains("Hitbox") && root.GetComponent<HealthManager>().IsAlive && !hitTargets.Contains(root.gameObject) && losCheck && !Controller.CompareTag(root.tag))
            {
                ulong hitId = root.GetComponent<NetworkObject>().NetworkObjectId;
                hitIds.Add(root.GetComponent<NetworkObject>().OwnerClientId);
                hitTargets.Add(root.gameObject);
            }
        }

        //apply effects to enemies
        if (hitIds.Count > 0)
            GroundSlamServerRpc(OwnerClientId, hitIds.ToArray(), ab2Speed);

        //apply effects to player
        Controller.statusEffects.Hover(ab2FloatTime);
        Invoke(nameof(Ability2DownForce), ab2FloatTime);

        onAbility2.Invoke();
    }

    private void Ability2DownForce()
    {
        Controller.statusEffects.Root(float.MaxValue);
        Controller.rb.velocity = Vector3.zero;
        Controller.rb.AddForce(Vector3.down * ab2Speed, ForceMode.Impulse);
    }

    private void StopAbility2()
    {
        hitTargets.Clear();
        Vector3 origin = Controller.transform.position;
        List<ulong> hitIds = new List<ulong>();

        Collider[] cols = Physics.OverlapSphere(origin, ab2Radius);

        foreach (Collider col in cols)
        {
            Transform root = col.transform.root;
            bool losCheck = !Physics.Raycast(origin, root.position - origin, Vector3.Distance(origin, root.position), GameManager.Instance.GroundMask);
            if (col.tag.Contains("Hitbox") && root.GetComponent<HealthManager>().IsAlive && !hitTargets.Contains(root.gameObject) && losCheck && !Controller.CompareTag(root.tag))
            {
                ulong hitId = root.GetComponent<NetworkObject>().NetworkObjectId;
                PlayerWeapon.DealDamageToTarget(hitId, ab1Damage);
                hitTargets.Add(root.gameObject);
            }
        }

        ability2CDTime = 0f;
        ab2Active = false;
        Controller.statusEffects.StopRoot();
    }

    #endregion

    #region Networking

    [ServerRpc]
    private void UpperCutServerRpc(ulong clientId, ulong[] targetIds, float duration, float distance, float moveSpeed)
    {
        UpperCutClientRpc(clientId, targetIds, duration, distance, moveSpeed);
    }

    [ClientRpc]
    private void UpperCutClientRpc(ulong clientId, ulong[] targetIds, float duration, float distance, float moveSpeed)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        PlayerController controller = NetworkManager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller;

        if (controller.statusEffects.rootImmunity) return;

        foreach (ulong id in targetIds)
        {
            if(id == NetworkManager.Singleton.LocalClientId)
            {
                //apply status effects
                controller.statusEffects.Root(duration);
                controller.statusEffects.Hover(duration);

                //move player to destination
                StartCoroutine(UpperCutMove(controller, distance, moveSpeed));

                return;
            }
        }
    }

    public IEnumerator UpperCutMove(PlayerController controller, float distance, float moveSpeed)
    {
        Vector3 startPos = controller.transform.position;
        Vector3 endPos;

        RaycastHit hit;
        Physics.Raycast(startPos, Vector3.up, out hit, distance + controller.GetComponent<CapsuleCollider>().height / 2, GameManager.Instance.GroundMask);

        if (hit.collider == null)
            endPos = startPos + Vector3.up * distance;
        else
            endPos = hit.point - Vector3.up * (controller.GetComponent<CapsuleCollider>().height / 2);

        float t = 0;

        while(t < 1)
        {
            t += Time.deltaTime * moveSpeed;

            controller.transform.position = Vector3.Lerp(startPos, endPos, Mathf.Min(1, t));

            yield return null;
        }

        controller.transform.position = endPos;
    }

    [ServerRpc]
    private void GroundSlamServerRpc(ulong clientId, ulong[] targetIds, float moveSpeed)
    {
        GroundSlamClientRpc(clientId, targetIds, moveSpeed);
    }

    [ClientRpc]
    private void GroundSlamClientRpc(ulong clientId, ulong[] targetIds, float speed)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return;

        PlayerController controller = NetworkManager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller;

        if (controller.statusEffects.rootImmunity) return;

        foreach (ulong id in targetIds)
        {
            if (id == NetworkManager.Singleton.LocalClientId)
            {
                //apply status effects
                controller.statusEffects.Root(float.MaxValue);

                //apply force
                controller.rb.velocity = Vector3.zero;
                controller.rb.AddForce(Vector3.down * speed, ForceMode.Impulse);

                //start corotine
                StartCoroutine(GroundSlamMove(controller));

                return;
            }
        }
    }

    private IEnumerator GroundSlamMove(PlayerController controller)
    {
        float t = 0;
        float maxT = 1000f;

        while (t < maxT)
        {
            if (controller.Grounded)
            {
                controller.statusEffects.StopRoot();
                yield break;
            }

            t += Time.deltaTime;

            yield return null;
        }
    }

    #endregion
}
