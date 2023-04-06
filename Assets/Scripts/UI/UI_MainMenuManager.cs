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
    public TMP_InputField playerNameInput;

    private void Awake()
    {
        hostButton.onClick.AddListener(HostOnClick);
        clientButton.onClick.AddListener(ClientOnClick);
    }

    private void HostOnClick()
    {
        //NetworkManager.Singleton.StartHost();

        GameManager.Instance.StartHost(playerNameInput.text);
    }

    private void ClientOnClick()
    {
        //NetworkManager.Singleton.StartClient();
        
        GameManager.Instance.StartClient(playerNameInput.text, joinCodeInput.text);
    }
}
