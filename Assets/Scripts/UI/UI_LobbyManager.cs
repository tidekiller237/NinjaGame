using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

public class UI_LobbyManager : MonoBehaviour
{
    bool connected;

    #region Connected

    [Header("Connected")]
    public GameObject connectedMenu;
    public GameObject hostOnlyMenu;
    public TextMeshProUGUI[] nameFields;
    public TextMeshProUGUI joinCodeText;
    public Button startButton;
    public Button disconnectButton;
    public Button copyCodeButton;

    #endregion

    #region Not Connected

    [Header("Not Connected")]
    public GameObject notConnectedMenu;
    public Button returnButton;

    #endregion

    private void Start()
    {
        startButton.onClick.AddListener(StartOnClick);
        disconnectButton.onClick.AddListener(DisconnectOnClick);
        copyCodeButton.onClick.AddListener(CopyButtonOnClick);
        returnButton.onClick.AddListener(ReturnButtonOnClick);
    }

    private void Update()
    {
        //connected activeation
        connected = NetworkManager.Singleton.IsClient || GameManager.AttemptingConnection;
        connectedMenu.SetActive(connected);
        hostOnlyMenu.SetActive(connected && NetworkManager.Singleton.IsHost);
        notConnectedMenu.SetActive(!connected);
        //show names
        for(int i = 0; i < nameFields.Length; i++)
        {
            string str = ""; 
            
            if(i < ConnectionManager.Instance.connectedPlayersNames.Count)
                str = ConnectionManager.Instance.connectedPlayersNames[i].ToString();

            if (str != null && str != "")
                nameFields[i].text = str;
            else
                nameFields[i].text = "Empty";
        }

        //show joincode to host
        joinCodeText.text = "Join Code: " + ConnectionManager.RelayJoinCode;
    }

    private void StartOnClick()
    {
        if (!ConnectionManager.IsHost) return;

        //start game
        ConnectionManager.Instance.RequestNetworkSceneChange("Development_Level");
    }

    private void DisconnectOnClick()
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().Disconnect();
        GameManager.Instance.RequestSceneChange("MainMenu");
    }

    private void ReturnButtonOnClick()
    {
        //NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().Disconnect();
        NetworkManager.Singleton.Shutdown();
        GameManager.Instance.RequestSceneChange("MainMenu");
    }

    private void CopyButtonOnClick()
    {
        GUIUtility.systemCopyBuffer = ConnectionManager.RelayJoinCode;
    }
}
