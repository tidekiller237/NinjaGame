using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class LevelLoadData : NetworkBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoadedListener;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedListener;
    }

    private void OnSceneLoadedListener(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Level Loaded");
        Debug.Log(scene.name);
        Debug.Log(mode);

        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().LevelLoadedSuccessfully();
    }

    private void Update()
    {
        if (!ConnectionManager.IsConnectedClient)
        {
            GameManager.Instance.RequestSceneChange("MainMenu");
        }
    }
}
