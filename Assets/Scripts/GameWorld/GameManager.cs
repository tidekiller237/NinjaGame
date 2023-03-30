using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    //singleton
    public static GameManager instance;
    public SceneState sceneState;

    public enum SceneState
    {
        MainMenu,
        Paused,
        InGame
    }

    private void Awake()
    {
        //singleton
        if (GameManager.instance == null)
            GameManager.instance = this;
        else
            Destroy(this);

        //persist throughout the game
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        sceneState = SceneState.MainMenu;

        #region Initialize Physics

        GamePhysics.Gravity = Vector3.up * -9.8f;
        GamePhysics.KineticFriction = 5f;

        Physics.gravity = GamePhysics.Gravity;

        #endregion
    }

    public void RequestSceneStateChange(SceneState desiredState)
    {
        //TODO: perform appropriate checks
        sceneState = desiredState;
    }
}

public static class GamePhysics
{
    public static Vector3 Gravity { get; set; }
    public static float KineticFriction { get; set; }
}
