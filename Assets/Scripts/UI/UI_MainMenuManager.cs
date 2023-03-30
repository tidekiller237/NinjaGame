using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class UI_MainMenuManager : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;
    public TMP_InputField joinCodeInput;
    public float attemptHostTime;
    float hostTime;
    public float attemptClientTime;
    float clientTime;

    private void Awake()
    {
        hostButton.onClick.AddListener(HostOnClick);
        clientButton.onClick.AddListener(ClientOnClick);
    }

    private void HostOnClick()
    {
        //NetworkManager.Singleton.StartHost();

        if (ConnectionManager.AuthPlayerID != "")
        {
            StopAllCoroutines();
            StartCoroutine(StartHost());
        }
    }

    private IEnumerator StartHost()
    {
        hostTime = 0f;
        ConnectionManager.Instance.InitializeHost();

        while(!ConnectionManager.IsHost && hostTime < attemptHostTime)
        {
            hostTime += Time.deltaTime;
            yield return null;
        }

        if (ConnectionManager.IsHost)
            GameManager.instance.RequestSceneStateChange(GameManager.SceneState.InGame);
        else
        {
            ConnectionManager.Instance.CancelConnection();
            Debug.LogError("Host start timed out. Host not started.");
        }
    }

    private void ClientOnClick()
    {
        //NetworkManager.Singleton.StartClient();

        if (ConnectionManager.AuthPlayerID != "" && joinCodeInput.text != "")
        {
            StopAllCoroutines();
            StartCoroutine(StartClient());
        }
    }

    private IEnumerator StartClient()
    {
        clientTime = 0f;
        ConnectionManager.Instance.InitializeClient(joinCodeInput.text);

        while (!ConnectionManager.IsConnected && clientTime < attemptClientTime)
        {
            clientTime += Time.deltaTime;
            yield return null;
        }

        if (ConnectionManager.IsConnected)
            GameManager.instance.RequestSceneStateChange(GameManager.SceneState.InGame);
        else
        {
            ConnectionManager.Instance.CancelConnection();
            Debug.LogError("Connection timed out.");
        }
    }
}
