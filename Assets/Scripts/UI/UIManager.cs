using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject UI_MainMenu;
    public GameObject UI_HUD;
    public GameObject UI_Pause;
    public GameObject UI_Lobby;
    public GameObject UI_LevelMenu;

    GameManager.SceneState lastState;

    private void Awake()
    {
        if (UIManager.Instance == null)
            UIManager.Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        DeactivateAllScreens();
        UpdateScreen();
    }

    private void Update()
    {
        if (GameManager.Instance.sceneState != lastState)
            UpdateScreen();
    }

    private void UpdateScreen()
    {
        DeactivateAllScreens();
        switch (GameManager.Instance.sceneState)
        {
            //only main menu is active
            case GameManager.SceneState.MainMenu:
                UI_MainMenu.SetActive(true);
                break;

            //only hud is active
            case GameManager.SceneState.InGame:
                UI_HUD.SetActive(true);
                break;

            //only pause menu is active
            case GameManager.SceneState.Paused:
                UI_Pause.SetActive(true);
                break;

            //only lobby is active
            case GameManager.SceneState.Lobby:
                UI_Lobby.SetActive(true);
                break;

            //only level menu is active
            case GameManager.SceneState.LevelMenu:
                UI_LevelMenu.SetActive(true);
                break;

            //nothing is active
            default:
                break;
        }

        lastState = GameManager.Instance.sceneState;
    }

    private void DeactivateAllScreens()
    {
        UI_MainMenu.SetActive(false);
        UI_HUD.SetActive(false);
        UI_LevelMenu.SetActive(false);
        UI_Lobby.SetActive(false);
    }
}
