using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;

public class GameManager : MonoBehaviour
{
    //singleton
    public static GameManager Instance;
    public SceneState sceneState;
    public SceneState lastSceneState;
    public string currentLevel;
    public LayerMask Team1Mask;
    public LayerMask Team2Mask;
    public LayerMask GroundMask;
    public LayerMask PlayerMask;

    public GameObject[] enableOnStart;

    public List<NetworkPlayer> team1 = new List<NetworkPlayer>();
    public List<NetworkPlayer> team2 = new List<NetworkPlayer>();

    [Header("Player Game Variables")]
    public float playerWalkSpeed;
    public int playerHealth;

    [Header("Keybinds")]
    public static KeyCode bind_jump = KeyCode.Space;
    public static KeyCode bind_crouch = KeyCode.LeftControl;
    public static KeyCode bind_abilityShift = KeyCode.LeftShift;
    public static KeyCode bind_ability1 = KeyCode.E;
    public static KeyCode bind_ability2 = KeyCode.Q;
    public static KeyCode bind_ability3 = KeyCode.R;
    public static KeyCode bind_spell1 = KeyCode.Z;
    public static KeyCode bind_spell2 = KeyCode.X;
    public static KeyCode bind_spell3 = KeyCode.C;
    public static KeyCode bind_primaryFire = KeyCode.Mouse0;
    public static KeyCode bind_secondaryFire = KeyCode.Mouse1;
    public static KeyCode bind_tertiaryFire = KeyCode.Mouse2;
    public static KeyCode bind_swapWeapon = KeyCode.Alpha2;
    public static KeyCode bind_kill = KeyCode.Backspace;
    public static KeyCode bind_pause = KeyCode.Escape;

    [Header("Game Related Tags")]
    public static string tag_team1 = "Team1";
    public static string tag_team2 = "Team2";
    public static string tag_spectator = "Spectator";

    public enum SceneState
    {
        MainMenu,
        Paused,
        InGame,
        Lobby,
        LevelMenu,  //this is the menu inside of the levels
        LevelLoading
    }

    private void Awake()
    {
        //singleton
        if (GameManager.Instance == null)
            GameManager.Instance = this;
        else
            Destroy(this);

        //persist throughout the game
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        sceneState = SceneState.MainMenu;

        foreach (GameObject obj in enableOnStart)
            obj.SetActive(true);

        #region Initialize Physics

        GamePhysics.Gravity = Vector3.up * -9.8f;
        GamePhysics.KineticFriction = 5f;

        Physics.gravity = GamePhysics.Gravity;

        #endregion
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu" && !ConnectionManager.IsHost && !ConnectionManager.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
            RequestSceneChange("MainMenu");
        }
    }

    #region Scene Changing

    private void SceneStateChange(SceneState desiredState)
    {
        //TODO: perform appropriate checks
        lastSceneState = sceneState;
        sceneState = desiredState;
    }

    public void RequestSceneChange(int sceneLoadIndex)
    {
        SceneManager.LoadScene(sceneLoadIndex, LoadSceneMode.Single);
    }

    public void RequestSceneChange(string sceneName)
    {
        switch (sceneName)
        {
            case "MainMenu":
                if(SceneManager.GetActiveScene().name != "MainMenu")
                    SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);

                currentLevel = "MainMenu";
                SceneStateChange(SceneState.MainMenu);
                break;
            case "Lobby":
                if (SceneManager.GetActiveScene().name != "MainMenu")
                    SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);

                currentLevel = "MainMenu";
                SceneStateChange(SceneState.Lobby);
                break;
            case "InGame":
                if((SceneManager.GetActiveScene().name.Contains("Level") || SceneManager.GetActiveScene().name.Contains("level")) 
                    && NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().character != null)
                {
                    SceneStateChange(SceneState.InGame);
                }
                break;
            case "Pause":
                if (SceneManager.GetActiveScene().name.Contains("Level") || SceneManager.GetActiveScene().name.Contains("level"))
                {
                    SceneStateChange(SceneState.Paused);
                }
                break;
            case "Unpause":
                if (sceneState == SceneState.Paused)
                    SceneStateChange(lastSceneState);
                break;
            default:
                SceneManager.sceneLoaded += OnLevelLoaded;

                try
                {
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                    SceneStateChange(SceneState.LevelLoading);
                }
                catch (Exception e)
                {
                    SceneManager.sceneLoaded -= OnLevelLoaded;
                    Debug.LogError($"Failed to load scene. Error message: {e.Message}");
                    SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                    SceneStateChange(SceneState.Lobby);
                }
                break;
        }
    }

    public void OnLevelLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnLevelLoaded;
        currentLevel = scene.name;
        Debug.Log("Level Loaded");
        Debug.Log(scene.name);
        Debug.Log(mode);

        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().LevelLoadedSuccessfully();
        SceneStateChange(SceneState.LevelMenu);
    }

    #endregion

    #region Main Menu

    public static bool AttemptingConnection;

    public float attemptHostTime;
    float hostTime;
    public float attemptClientTime;
    float clientTime;

    public void StartHost(string playerName)
    {
        if (ConnectionManager.AuthPlayerID != "" && playerName != "")
        {
            StopAllCoroutines();
            StartCoroutine(StartHostC(playerName));
        }
    }

    private IEnumerator StartHostC(string playerName)
    {
        AttemptingConnection = true;

        hostTime = 0f;
        ConnectionManager.Instance.InitializeHost();

        while (!ConnectionManager.IsHost && hostTime < attemptHostTime)
        {
            hostTime += Time.deltaTime;
            yield return null;
        }

        if (ConnectionManager.IsHost)
        {
            GameManager.Instance.SceneStateChange(GameManager.SceneState.InGame);
            ConnectionManager.Instance.SetPlayerIdAndName(NetworkManager.Singleton.LocalClientId, playerName);
            RequestSceneChange("Lobby");
        }
        else
        {
            ConnectionManager.Instance.CancelConnection();
            Debug.LogError("Host start timed out. Host not started.");
        }

        AttemptingConnection = false;
    }

    public void StartClient(string playerName, string joinCode)
    {
        if (ConnectionManager.AuthPlayerID != "" && playerName != "" && joinCode != "")
        {
            StopAllCoroutines();
            StartCoroutine(StartClientC(playerName, joinCode));
        }
    }

    private IEnumerator StartClientC(string playerName, string joinCode)
    {
        AttemptingConnection = true;

        clientTime = 0f;
        ConnectionManager.Instance.InitializeClient(joinCode);

        while (!ConnectionManager.IsConnectedClient && clientTime < attemptClientTime)
        {
            clientTime += Time.deltaTime;
            yield return null;
        }

        if (ConnectionManager.IsConnectedClient)
        {
            GameManager.Instance.SceneStateChange(GameManager.SceneState.InGame);
            ConnectionManager.Instance.SetPlayerIdAndName(NetworkManager.Singleton.LocalClientId, playerName);
            RequestSceneChange("Lobby");
        }
        else
        {
            ConnectionManager.Instance.CancelConnection();
            Debug.LogError("Connection timed out.");
        }

        AttemptingConnection = false;
    }

    #endregion

    #region Teams

    public void SetPlayerToTeam(NetworkPlayer player, int team)
    {
        if(team == 1)
        {
            if (!team1.Contains(player))
            {
                if (team2.Contains(player))
                    team2.Remove(player);

                team1.Add(player);
            }
        }
        else if(team == 2)
        {
            if (!team2.Contains(player))
            {
                if (team1.Contains(player))
                    team1.Remove(player);

                team2.Add(player);
            }
        }
    }

    public void RemovePlayerFromTeam(NetworkPlayer player, int team)
    {
        if(team == 1 && team1.Contains(player))
        {
            team1.Remove(player);
        }
        else if(team == 2 && team2.Contains(player))
        {
            team2.Remove(player);
        }
    }

    #endregion
}

public static class GamePhysics
{
    public static Vector3 Gravity { get; set; }
    public static float KineticFriction { get; set; }
}
