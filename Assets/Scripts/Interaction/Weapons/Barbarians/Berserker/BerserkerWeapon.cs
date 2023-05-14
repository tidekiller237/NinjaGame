using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BerserkerWeapon : BarbarianWeapon
{
    List<GameObject> hitTargets = new List<GameObject>();

    [Header("Melee")]
    public bool enableMelee;

    [Header("Melee Primary")]
    public int meleePrimaryDamage;
    public float meleePrimarySwingDuration;
    public float meleePrimaryRadius;
    public float meleePrimaryDistance;
    public float meleePrimaryCooldown;
    bool meleeCheck;
    bool canMeleePrimary;

    [Header("Melee Secondary")]
    public int mSecondaryDamage;
    public float mSecondaryRadius;
    public float mSecondaryDistance;
    public float mSecondaryDuration;
    public float mSecondaryPushForce;
    public float mSecondarySpeed;
    public float mSecondaryCooldown;
    [HideInInspector] public float mSecondaryCDTime;
    bool canMeleeSecondary;
    bool mSecondaryActive;
    float mSecondaryActiveTime;
    Vector3 mSecondaryDestination;
    Vector3 mSecondaryDirection;

    protected override void Awake()
    {
        OnAwake();
    }

    private void Start()
    {
        OnStart();

        canMeleePrimary = true;
        canMeleeSecondary = true;
    }

    protected override void InitializeCooldowns()
    {
        base.InitializeCooldowns();

        mSecondaryCDTime = mSecondaryCooldown;

        //technically not a cooldown
        mSecondaryActiveTime = mSecondaryDuration;
    }

    protected override void Update()
    {
        OnUpdate();

        if (!IsOwner) return;

        //secondary removes control
        SetControl(!mSecondaryActive);

        if (controller != null && melee)
        {
            if (meleeCheck)
                MeleeCheck();
            else if (mSecondaryActive)
            {
                SecondaryEffect();

                //if (mSecondaryActiveTime < mSecondaryDuration)
                //{
                //    mSecondaryActiveTime += Time.deltaTime;
                //    SecondaryEffect();
                //}
                //else
                //    StopSecondaryAttack();
            }
        }
    }

    protected override void Cooldowns()
    {
        base.Cooldowns();

        //handle melee related cooldowns
        if (mSecondaryCDTime != mSecondaryCooldown)
            mSecondaryCDTime = Mathf.Min(mSecondaryCooldown, mSecondaryCDTime + Time.deltaTime);
        else if(!mSecondaryActive)
            canMeleeSecondary = true;
    }

    protected override void Inputs()
    {
        base.Inputs();

        if (melee)
        {
            if (enableMeleePrimary && canMeleePrimary && Input.GetKey(GameManager.bind_primaryFire))
                MeleePrimary();

            if (enableMeleeSecondary && canMeleeSecondary && Input.GetKey(GameManager.bind_secondaryFire))
                StartSecondaryAttack();
        }
    }

    protected override void SwapWeapon()
    {
        //canMeleePrimary = false;

        base.SwapWeapon();
    }

    protected override void SwapWeaponEnd()
    {
        canMeleePrimary = true;

        base.SwapWeaponEnd();
    }

    #region Primary Attack

    private void MeleePrimary()
    {
        canMeleePrimary = false;
        MeleeStart();
        Invoke(nameof(MeleeStop), meleePrimarySwingDuration);
        Invoke(nameof(ResetMeleePrimary), meleePrimaryCooldown);

        OnAttack.Invoke();
    }

    private void MeleeStart()
    {
        meleeCheck = true;
        hitTargets.Clear();
    }

    private void MeleeStop()
    {
        meleeCheck = false;
    }

    private void ResetMeleePrimary()
    {
        canMeleePrimary = true;
    }

    private void MeleeCheck()
    {
        GameObject[] hits = MeleeCheck(controller.mainCamera.transform, meleePrimaryDistance, meleePrimaryRadius);

        foreach (GameObject obj in hits)
        {
            if (!hitTargets.Contains(obj) && obj.gameObject != controller.gameObject && obj.tag != controller.gameObject.tag)
            {
                DealDamageToTarget(obj.GetComponent<NetworkObject>().NetworkObjectId, meleePrimaryDamage);
                hitTargets.Add(obj);
            }
        }
    }

    #endregion

    #region Secondary Attack

    private void StartSecondaryAttack()
    {
        if (controller.statusEffects.rooted) return;

        canMeleeSecondary = false;
        mSecondaryActive = true;
        hitTargets.Clear();
        controller.TakeControl(false, false, false, false);
        controller.StopCrouch();
        controller.rb.velocity = Vector3.zero;
        mSecondaryDirection = controller.transform.forward;

        //check if it's a valid position
        float height = controller.GetComponent<CapsuleCollider>().height - 0.2f;
        float radius = controller.GetComponent<CapsuleCollider>().radius;
        RaycastHit hit;
        Physics.CapsuleCast(controller.transform.position - Vector3.up * (height / 2), controller.transform.position + Vector3.up * (height / 2), radius, controller.transform.forward, out hit, mSecondaryDistance, GameManager.Instance.GroundMask);

        if(hit.collider != null)
        {
            float dist = Vector3.Project((hit.point - controller.transform.position), controller.transform.forward).magnitude - radius;
            mSecondaryDestination = controller.transform.position + controller.transform.forward * dist;
        }
        else
            mSecondaryDestination = controller.transform.position + controller.transform.forward * mSecondaryDistance;

        StartCoroutine(SecondaryMove(mSecondaryDestination, mSecondarySpeed));

        mSecondaryActiveTime = 0f;

        OnSecondaryAttack.Invoke();
    }

    private IEnumerator SecondaryMove(Vector3 destination, float speed)
    {
        float t = 0;
        Vector3 startPos = controller.transform.position;

        while(t < 1)
        {
            t += Time.deltaTime * speed;

            controller.transform.position = Vector3.Lerp(startPos, destination, Mathf.Min(1, t));

            yield return null;
        }

        controller.transform.position = destination;
        
        StopSecondaryAttack();
    }

    private IEnumerator SecondaryMove(PlayerController control, Vector3 destination, float speed, float duration, Vector3 force)
    {
        float t = 0;
        Vector3 startPos = control.transform.position;

        while (t < 1)
        {
            t += Time.deltaTime * speed;

            control.transform.position = Vector3.Lerp(startPos, destination, Mathf.Min(1, t));

            yield return null;
        }

        control.transform.position = destination;

        StopSecondaryHitEffect(control, duration, force);
    }

    private void StopSecondaryAttack()
    {
        mSecondaryActive = false;
        mSecondaryCDTime = 0f;
        controller.ReturnControl();

        OnSecondaryAttackEnd.Invoke();
    }

    private void SecondaryEffect()
    {
        if (controller.statusEffects.stunned)
        {
            StopSecondaryAttack();
            return;
        }

        SecondaryAttackCheck();
    }

    private void SecondaryAttackCheck()
    {
        Vector3 origin = controller.transform.position;
        Collider[] cols = Physics.OverlapSphere(origin + ((mSecondaryDestination - origin).normalized * mSecondaryRadius), mSecondaryRadius);

        foreach(Collider col in cols)
        {
            Transform root = col.transform.root;
            bool losCheck = !Physics.Raycast(origin, root.position - origin, Vector3.Distance(origin, root.position), meleeBlockLayers);
            if (col.tag.Contains("Hitbox") && root.GetComponent<HealthManager>().IsAlive && !hitTargets.Contains(root.gameObject) && losCheck && !controller.CompareTag(root.tag))
            {
                DealDamageToTarget(root.GetComponent<NetworkObject>().NetworkObjectId, mSecondaryDamage);
                hitTargets.Add(root.gameObject);
                SecondaryHitServerRpc(OwnerClientId, root.GetComponent<NetworkObject>().OwnerClientId, mSecondaryDestination, mSecondarySpeed, mSecondaryDuration, mSecondaryDirection * mSecondaryPushForce);
            }
        }
    }

    [ServerRpc]
    private void SecondaryHitServerRpc(ulong clientId, ulong targetId, Vector3 destination, float speed, float duration, Vector3 force)
    {
        SecondaryHitClientRpc(clientId, targetId, destination, speed, duration, force);
    }

    [ClientRpc]
    private void SecondaryHitClientRpc(ulong clientId, ulong targetId, Vector3 destination, float speed, float duration, Vector3 force)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId || targetId != NetworkManager.Singleton.LocalClientId) return;

        PlayerController control = NetworkManager.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller;

        if (control.statusEffects.rootImmunity) return;

        StartCoroutine(SecondaryMove(control, destination, speed, duration, force));
    }

    private void StopSecondaryHitEffect(PlayerController control, float duration, Vector3 force)
    {
        control.ReturnControl();

        control.rb.velocity = Vector3.zero;
        control.statusEffects.Root(duration);
        control.statusEffects.Slip(duration);

        control.rb.AddForce(force, ForceMode.Impulse);
    }

    #endregion
}
