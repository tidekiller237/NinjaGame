using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject UI_MainMenu;
    public GameObject UI_HUD;

    GameManager.SceneState lastState;

    private void Awake()
    {
        if (UIManager.Instance == null)
            UIManager.Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        DeactivateAllScreens();
        UpdateScreen();
    }

    private void Update()
    {
        if (GameManager.instance.sceneState != lastState)
            UpdateScreen();
    }

    private void UpdateScreen()
    {
        DeactivateAllScreens();
        switch (GameManager.instance.sceneState)
        {
            //only main menu is active
            case GameManager.SceneState.MainMenu:
                UI_MainMenu.SetActive(true);
                break;

            //only hud is active
            case GameManager.SceneState.InGame:
                UI_HUD.SetActive(true);
                break;

            //nothing is active
            default:
                break;
        }

        lastState = GameManager.instance.sceneState;
    }

    private void DeactivateAllScreens()
    {
        UI_MainMenu.SetActive(false);
        UI_HUD.SetActive(false);
    }
}
