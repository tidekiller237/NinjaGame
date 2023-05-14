using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UI_HUDManager : MonoBehaviour
{
    PlayerController controller;

    public TextMeshProUGUI speedText;
    public TextMeshProUGUI stateText;

    [Header("Hitmarker")]
    public GameObject hitMarker;

    [Header("Health")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    public float healthUpdateSpeed;
    int health;
    int lastHealth;

    [Header("Dash")]
    public Slider dashCooldownSlider;
    public float dashCooldownLinger;
    float lastDashCooldown;
    float dashCooldownUpdateTime;

    private void Start()
    {
        hitMarker.SetActive(false);
    }

    private void Update()
    {
        controller = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().controller;

        Health();
        SpeedText();
        StateText();
        UpdateDashCooldownSlider();
    }

    private void Health()
    {
        HealthManager manager = controller.GetComponent<HealthManager>();

        if(manager.currentHealth.Value != lastHealth)
        {
            StopAllCoroutines();
            StartCoroutine(UpdateHealth(manager.currentHealth.Value, healthUpdateSpeed));
            lastHealth = manager.currentHealth.Value;
        }

        healthSlider.maxValue = manager.maxHealth;
        healthSlider.value = health;
        healthText.text = $"{manager.currentHealth.Value} / {manager.maxHealth}";
    }

    private IEnumerator UpdateHealth(int newValue, float updateSpeed = 1)
    {
        float t = 0;
        int oldValue = health;

        while(t < 1)
        {
            t += Time.deltaTime;

            health = (int)Mathf.Lerp(oldValue, newValue, Mathf.Min(t, 1));

            yield return null;
        }

        health = newValue;
    }

    private void SpeedText()
    {
        if(controller != null)
            speedText.text = "Speed: " + string.Format("{0:0.00}", controller.GetComponent<Rigidbody>().velocity.magnitude);
    }

    private void StateText()
    {
        if (controller == null) return;

        switch (controller.moveState)
        {
            case PlayerController.MovementState.Walking:
                stateText.text = "Walking";
                break;
            case PlayerController.MovementState.Air:
                stateText.text = "Air";
                break;
            case PlayerController.MovementState.Ability:
                stateText.text = "Ability";
                break;
            case PlayerController.MovementState.WallRunning:
                stateText.text = "Wall Running";
                break;
            case PlayerController.MovementState.Climbing:
                stateText.text = "Climbing";
                break;
            case PlayerController.MovementState.Crouching:
                stateText.text = "Crouching";
                break;
            default:
                stateText.text = "None";
                break;
        }
    }

    public void DisplayHitmarker(float duration)
    {
        hitMarker.SetActive(true);
        Invoke(nameof(DisableHitmarker), duration);
    }

    private void DisableHitmarker()
    {
        hitMarker.SetActive(false);
    }

    private void UpdateDashCooldownSlider()
    {
        if (controller == null) return;

        if (controller.DashCooldownTime != lastDashCooldown)
        {
            dashCooldownSlider.gameObject.SetActive(true);
            dashCooldownSlider.value = controller.DashCooldownTime / controller.dashCooldown;
            dashCooldownUpdateTime = 0f;
            lastDashCooldown = controller.DashCooldownTime;
        }
        else if (dashCooldownUpdateTime < dashCooldownLinger)
            dashCooldownUpdateTime += Time.deltaTime;
        else if(controller.canDash)
            dashCooldownSlider.gameObject.SetActive(false);
    }
}
