using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class GenericAnimationHandler : NetworkBehaviour
{
    PlayerController controller;
    Weapon weapon;
    Class playerClass;
    Ability ability;
    public Transform headTracker;
    public Transform headTransform;
    public Animator thirdPersonAnim;
    public Animator firstPersonAnim;

    private void Start()
    {
        if (!IsOwner) return;

        controller = GetComponent<PlayerController>();
        weapon = controller.Weapon;
        playerClass = controller.PlayerClass;
        ability = controller.Ability;
        weapon.OnAttack.AddListener(OnAttackListener);
        controller.GetComponent<HealthManager>().OnDeath.AddListener(OnDeathListener);
        controller.GetComponent<HealthManager>().OnBodyCleanUp.AddListener(OnCleanupListener);
        controller.OnDash.AddListener(OnDashListener);
    }

    private void Update()
    {
        if (!IsOwner) return;

        Rigidbody rb = transform.root.GetComponent<Rigidbody>();

        Vector3 velocity = new Vector3(controller.rb.velocity.x, 0f, controller.rb.velocity.z);
        SetFloatServerRpc(OwnerClientId, "MoveSpeed", velocity.magnitude);
        
        float x = Vector3.Dot(velocity, Vector3.Cross(controller.transform.forward, Vector3.up));
        float z = Vector3.Dot(velocity, controller.transform.forward);
        SetFloatServerRpc(OwnerClientId, "MoveSpeedX", x);
        SetFloatServerRpc(OwnerClientId, "MoveSpeedZ", z);

        headTracker.position = headTransform.position + controller.mainCamera.transform.forward;

        SetBoolServerRpc(OwnerClientId, "Melee", weapon.melee);
        SetBoolServerRpc(OwnerClientId, "Crouched", controller.crouching);
        //SetBoolServerRpc(OwnerClientId, "PreparingSpell", ability.preparingSpell);

        if (firstPersonAnim.gameObject.activeInHierarchy)
        {
            firstPersonAnim.SetBool("Melee", weapon.melee);
            //firstPersonAnim.SetBool("PreparingSpell", ability.preparingSpell || ability.castingSpell);
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        if (firstPersonAnim.gameObject.activeInHierarchy)
            firstPersonAnim.ResetTrigger("Attack");
    }

    public void OnAttackListener()
    {
        if (!IsOwner) return;

        SetTriggerServerRpc(OwnerClientId, "Attack");

        if(firstPersonAnim.gameObject.activeInHierarchy)
            firstPersonAnim.SetTrigger("Attack");
    }

    public void OnDeathListener()
    {
        if(!IsOwner) return;

        SetTriggerServerRpc(OwnerClientId, "Death");
    }

    public void OnCleanupListener()
    {
        if(!IsOwner) return;

        SetTriggerServerRpc(OwnerClientId, "Respawn");
    }

    public void OnDashListener()
    {
        if (!IsOwner) return;

        SetTriggerServerRpc(OwnerClientId, "Dash");
    }

    private void ResetTriggers()
    {
        //reset triggers
        thirdPersonAnim.ResetTrigger("Attack");
        thirdPersonAnim.ResetTrigger("Death");
        thirdPersonAnim.ResetTrigger("Respawn");
    }

    [ServerRpc]
    private void SetTriggerServerRpc(ulong clientId, FixedString64Bytes name)
    {
        SetTriggerClientRpc(clientId, name);
    }

    [ClientRpc]
    private void SetTriggerClientRpc(ulong clientId, FixedString64Bytes name)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId) return;

        thirdPersonAnim.SetTrigger(name.ToString());
        Invoke(nameof(ResetTriggers), 0.25f);
    }

    [ServerRpc]
    private void SetBoolServerRpc(ulong clientId, FixedString64Bytes name, bool value)
    {
        SetBoolClientRpc(clientId, name, value);
    }

    [ClientRpc]
    private void SetBoolClientRpc(ulong clientId, FixedString64Bytes name, bool value)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId) return;

        thirdPersonAnim.SetBool(name.ToString(), value);
    }

    [ServerRpc]
    private void SetFloatServerRpc(ulong clientId, FixedString64Bytes name, float value)
    {
        SetFloatClientRpc(clientId, name, value);
    }

    [ClientRpc]
    private void SetFloatClientRpc(ulong clientId, FixedString64Bytes name, float value)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId) return;

        thirdPersonAnim.SetFloat(name.ToString(), value);
    }
}
