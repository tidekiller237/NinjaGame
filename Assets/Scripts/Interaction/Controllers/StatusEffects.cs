using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StatusEffects : MonoBehaviour
{
    /* Status Effects:
     * 
     * Stunned - Does not take input for duration
     * Rooted - Cannot move 
     * Hover - Is not effected by gravity
     * Slip - Drag is disabled
     */

    //If there is a positive duration given, effect expires
    //  after duration, otherwise, it lasts until removed.
    //  To be safe, no effect can last more than 30s.

    PlayerController controller;

    [HideInInspector] public bool stunned;
    float stunDuration;
    [HideInInspector]public bool stunImmunity;
    float stunImmunityDuration;
    [HideInInspector] public UnityEvent OnStun;

    [HideInInspector] public bool rooted;
    float rootDuration;
    [HideInInspector] public bool rootImmunity;
    float rootImmunityDuration;
    [HideInInspector] public UnityEvent OnRoot;

    [HideInInspector] public bool hovered;
    float hoverDuration;
    [HideInInspector] public bool hoverImmunity;
    float hoverImmunityDuration;
    [HideInInspector] public UnityEvent OnHover;

    [HideInInspector] public bool slippery;
    float slipDuration;
    [HideInInspector] public bool slipImmunity;
    float slipImmunityDuration;
    [HideInInspector] public UnityEvent OnSlip;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        GetComponent<HealthManager>().OnDeath.AddListener(OnDeathListener);
    }

    private void Start()
    {
        stunned = false;
        stunImmunity = false;
        rooted = false;
        rootImmunity = false;
        hovered = false;
        hoverImmunity = false;
        slippery = false;
        slipImmunity = false;
    }

    private void Update()
    {
        HandleDuration();
        HandleEffects();
    }

    private void HandleDuration()
    {
        if (stunDuration != 0f)
            stunDuration = Mathf.Max(0f, stunDuration - Time.deltaTime);

        if (stunImmunityDuration != 0f)
            stunImmunityDuration = Mathf.Max(0f, stunImmunityDuration - Time.deltaTime);

        if (rootDuration != 0f)
            rootDuration = Mathf.Max(0f, rootDuration - Time.deltaTime);

        if (rootImmunityDuration != 0f)
            rootImmunityDuration = Mathf.Max(0f, rootImmunityDuration - Time.deltaTime);

        if (hoverDuration != 0f)
            hoverDuration = Mathf.Max(0f, hoverDuration - Time.deltaTime);

        if (hoverImmunityDuration != 0f)
            hoverImmunityDuration = Mathf.Max(0f, hoverImmunityDuration - Time.deltaTime);

        if (slipDuration != 0f)
            slipDuration = Mathf.Max(0f, slipDuration - Time.deltaTime);

        if (slipImmunityDuration != 0f)
            slipImmunityDuration = Mathf.Max(0f, slipImmunityDuration - Time.deltaTime);
    }

    private void HandleEffects()
    {
        //effects

        if (stunDuration != 0)
            StunEffect();
        else if (stunDuration == 0 && stunned)
            StopStun();

        if (rootDuration != 0)
            RootEffect();
        else if (rootDuration == 0 && rooted)
            StopRoot();

        if (hoverDuration != 0)
            HoverEffect();
        else if (hoverDuration == 0 && hovered)
            StopHover();

        if (slipDuration != 0)
            SlipEffect();
        else if (slipDuration == 0 && slippery)
            StopSlip();

        //immunities

        if (stunImmunityDuration == 0 && stunImmunity)
            StopStunImmunity();

        if (rootImmunityDuration == 0 && rootImmunity)
            StopRootImmunity();

        if (hoverImmunityDuration == 0 && hoverImmunity)
            StopHoverImmunity();

        if (slipImmunityDuration == 0 && slipImmunity)
            StopSlipImmunity();
    }

    //reset all status effects on death
    public void OnDeathListener()
    {
        stunDuration = 0;
        stunImmunityDuration = 0;
        rootDuration = 0;
        rootImmunityDuration = 0;
        hoverDuration = 0;
        hoverImmunityDuration = 0;
        slipDuration = 0;
        slipImmunityDuration = 0;
    }

    #region Stun

    public void Stun(float duration)
    {
        if (stunImmunity) return;

        stunDuration = Mathf.Max(stunDuration, duration);
        OnStun.Invoke();
    }

    public void StunEffect()
    {
        stunned = true;
    }

    public void StopStun()
    {
        stunned = false;
        stunDuration = 0f;
    }

    public void StunImmunity(float duration)
    {
        stunned = false;
        stunImmunity = true;
        stunImmunityDuration = Mathf.Max(stunImmunityDuration, duration);
    }

    public void StopStunImmunity()
    {
        stunImmunity = false;
        stunImmunityDuration = 0f;
    }

    #endregion

    #region Root

    public void Root(float duration)
    {
        if (rootImmunity) return;

        rootDuration = Mathf.Max(rootDuration, duration);

        OnRoot.Invoke();
    }

    public void RootEffect()
    {
        rooted = true;
    }

    public void StopRoot()
    {
        rooted = false;
        rootDuration = 0f;
    }

    public void RootImmunity(float duration)
    {
        rooted = false;
        rootImmunity = true;
        rootImmunityDuration = Mathf.Max(rootImmunityDuration, duration);
    }

    public void StopRootImmunity()
    {
        rootImmunity = false;
        rootImmunityDuration = 0f;
    }

    #endregion

    #region Hover

    public void Hover(float duration)
    {
        if (hoverImmunity) return;

        hoverDuration = Mathf.Max(hoverDuration, duration);

        OnHover.Invoke();
    }

    public void HoverEffect()
    {
        hovered = true;
    }

    public void StopHover()
    {
        hovered = false;
        hoverDuration = 0f;
    }

    public void HoverImmunity(float duration)
    {
        hovered = false;
        hoverImmunity = true;
        hoverImmunityDuration = Mathf.Max(hoverImmunityDuration, duration);
    }

    public void StopHoverImmunity()
    {
        hoverImmunity = false;
        hoverImmunityDuration = 0f;
    }

    #endregion

    #region Slip

    public void Slip(float duration)
    {
        if (slipImmunity) return;

        slipDuration = Mathf.Max(slipDuration, duration);
        OnSlip.Invoke();
    }

    public void SlipEffect()
    {
        slippery = true;
    }

    public void StopSlip()
    {
        slippery = false;
        slipDuration = 0f;
    }

    public void SlipImmunity(float duration)
    {
        slippery = false;
        slipImmunity = true;
        slipImmunityDuration = Mathf.Max(slipImmunityDuration, duration);
    }

    public void StopSlipImmunity()
    {
        slipImmunity = false;
        slipImmunityDuration = 0f;
    }

    #endregion
}
