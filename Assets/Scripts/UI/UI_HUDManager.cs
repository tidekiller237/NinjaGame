using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HUDManager : MonoBehaviour
{
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI stateText;

    public GameObject hitMarker;

    private void Start()
    {
        hitMarker.SetActive(false);
    }

    private void Update()
    {
        SpeedText();
        StateText();
    }

    private void SpeedText()
    {
        if(PlayerController.Instance != null)
            speedText.text = "Speed: " + string.Format("{0:0.00}", PlayerController.Instance.GetComponent<Rigidbody>().velocity.magnitude);
    }

    private void StateText()
    {
        if (PlayerController.Instance == null) return;

        switch (PlayerController.Instance.moveState)
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
}
