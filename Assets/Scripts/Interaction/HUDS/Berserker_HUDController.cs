using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Berserker_HUDController : NetworkBehaviour
{
    [Header("Components")]
    public Barbarian playerClass;
    public BerserkerWeapon playerWeapon;
    public Berserker playerAbility;

    [Header("UI")]
    public GameObject hudPrefab;
    HUD_Berserker hudInstance;

    [Header("Variables")]
    public float rageSliderUpdateDuration;
    public float activeAlpha;
    public float inactiveAlpha;

    private void Start()
    {
        if (!IsOwner) return;

        GameObject parent = GameObject.Find("UI").GetComponent<UIManager>().UI_HUD;
        hudInstance = Instantiate(hudPrefab, parent.transform).GetComponent<HUD_Berserker>();
        hudInstance.rageSliderUpdateDuration = rageSliderUpdateDuration;
        hudInstance.activeAlpha = activeAlpha;
        hudInstance.inactiveAlpha = inactiveAlpha;

        playerClass.onRageChanged.AddListener(OnRageChangedListener);
    }

    private void Update()
    {
        if (!IsOwner) return;

        Cooldowns();

        hudInstance.SetMode(playerWeapon.melee);
    }

    private void Cooldowns()
    {
        //ranged secondary fire
        hudInstance.UpdateAbilityCooldown(playerWeapon.secondaryFireCDTime, playerWeapon.secondaryFireCooldown, 0);
        hudInstance.UpdateAbilityCooldown(playerWeapon.mSecondaryCDTime, playerWeapon.mSecondaryCooldown, 1);
        hudInstance.UpdateAbilityCooldown(playerAbility.ability1CDTime, playerAbility.ability1Cooldown, 2);
        hudInstance.UpdateAbilityCooldown(playerAbility.ability2CDTime, playerAbility.ability2Cooldown, 3);
        hudInstance.UpdateAbilityCooldown(playerAbility.ability3CDTime, playerAbility.ability3Cooldown, 4);
        hudInstance.UpdateAbilityCooldown(playerAbility.classAbilityCDTime, playerAbility.classAbilityCooldown, 5);

        //set ability activity
        hudInstance.SetAbilityActiveState(playerAbility.ab2HeightCheck, 3);
    }

    public override void OnDestroy()
    {
        Destroy(hudInstance);
        base.OnDestroy();
    }

    public void OnRageChangedListener(int value)
    {
        hudInstance.UpdateRageSlider(value, 1f);
    }
}
