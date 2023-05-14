using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD_Berserker : MonoBehaviour
{
    [Header("Rage Slider")]
    public Slider rageSlider;
    public Image rageSliderFill;
    public TextMeshProUGUI rageText;
    public float rageSliderUpdateDuration;
    public float activeAlpha;
    public float inactiveAlpha;
    float rageSliderTrueValue;
    float rageSliderUpdateTime;
    bool rageSliderUpdateStopped;

    [Header("Ability Cooldowns")]
    public Slider[] abilitySliders;
    public Image[] abilityFills;
    public TextMeshProUGUI[] abilityTexts;
    float[] abilityValues;

    bool melee;

    private void Start()
    {
        UpdateRageSlider(0, 100f);
        abilityValues = new float[abilitySliders.Length];
    }

    private void Update()
    {
        if (rageSliderUpdateTime < rageSliderUpdateDuration)
            rageSliderUpdateTime += Time.deltaTime;
        else if (!rageSliderUpdateStopped)
            EndRageSliderUpdate();

        abilitySliders[0].gameObject.SetActive(!melee);
        abilitySliders[1].gameObject.SetActive(melee);
    }

    #region Interface

    public void UpdateRageSlider(int newValue, float updateSpeed)
    {
        StopAllCoroutines();
        StartCoroutine(RageSliderUpdate(newValue, updateSpeed));
    }

    /// <summary>
    /// Indexes:
    /// [0] RangedWeaponAbility;
    /// [1] MeleeWeaponAbility;
    /// [2] Ability1;
    /// [3] Ability2;
    /// [4] Ability3;
    /// [5] ClassAbility;
    /// [...] Everything else;
    /// </summary>
    public void UpdateAbilityCooldown(float value, float maxValue, int index)
    {
        if (index > abilitySliders.Length) return;

        float currentValue = value / maxValue;
        abilityValues[index] = Mathf.Clamp(currentValue, 0f, 1f);
        abilitySliders[index].value = abilityValues[index];

        if (currentValue < 1)
        {
            abilityTexts[index].gameObject.SetActive(true);
            abilityTexts[index].text = ((int)(maxValue - value) + 1).ToString();
        }
        else
        {
            abilityTexts[index].gameObject.SetActive(false);
        }
    }

    public void SetAbilityActiveState(bool value, int index)
    {
        if(index > abilitySliders.Length) return;

        if (value)
            abilityFills[index].color = new(abilityFills[index].color.r, abilityFills[index].color.g, abilityFills[index].color.b, activeAlpha);
        else
            abilityFills[index].color = new(abilityFills[index].color.r, abilityFills[index].color.g, abilityFills[index].color.b, inactiveAlpha);
    }

    public void SetMode(bool meleeMode)
    {
        melee = meleeMode;
    }

    #endregion

    #region Functionality

    private IEnumerator RageSliderUpdate(float newValue, float updateSpeed)
    {
        StartRageSliderUpdate();

        float oldValue = rageSlider.value;
        float t = 0;
        rageSliderTrueValue = newValue;

        while(t < 1)
        {
            t += Time.deltaTime * updateSpeed;
            rageSlider.value = Mathf.Lerp(oldValue, rageSliderTrueValue, Mathf.Min(t, 1));
            rageText.text = rageSlider.value.ToString();

            yield return null;
        }

        rageSlider.value = rageSliderTrueValue;

        rageSliderUpdateTime = 0f;
        rageSliderUpdateStopped = false;
    }

    private void StartRageSliderUpdate()
    {
        rageSliderUpdateStopped = true;

        Image backgroundImage = rageSlider.transform.GetChild(0).GetComponent<Image>();
        Image fillImage = rageSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>();

        backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, activeAlpha);
        fillImage.color = new Color(fillImage.color.r, fillImage.color.g, fillImage.color.b, activeAlpha);
    }

    private void EndRageSliderUpdate()
    {
        rageSliderUpdateStopped = true;

        Image backgroundImage = rageSlider.transform.GetChild(0).GetComponent<Image>();
        Image fillImage = rageSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>();

        backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, inactiveAlpha);
        fillImage.color = new Color(fillImage.color.r, fillImage.color.g, fillImage.color.b, inactiveAlpha);
    }

    #endregion
}
