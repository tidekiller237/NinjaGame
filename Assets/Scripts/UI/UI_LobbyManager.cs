using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

public class UI_LobbyManager : NetworkBehaviour
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

    public List<string> playerNames;

    #endregion

    #region Not Connected

    [Header("Not Connected")]
    public GameObject notConnectedMenu;
    public Button ncReturnButton;

    public void NCReturnButtonOnClick()
    {
        GameManager.Instance.RequestSceneStateChange(GameManager.SceneState.MainMenu);
    }

    #endregion

    private void Start()
    {
        //if(IsServer)
        //    playerNames = new NetworkList<FixedString64Bytes>();

        ncReturnButton.onClick.AddListener(NCReturnButtonOnClick);
        ConnectionManager.onPlayerUdate.AddListener(UpdatePlayerNames);
    }

    private void Update()
    {
        //connected activeation
        connected = NetworkManager.Singleton.IsClient || GameManager.AttemptingConnection;
        connectedMenu.SetActive(connected);
        hostOnlyMenu.SetActive(connected && NetworkManager.Singleton.IsHost);
        notConnectedMenu.SetActive(!connected);

        //show names
        for(int i = 0; i < Mathf.Min(playerNames.Count, nameFields.Length); i++)
        {
            if (playerNames[i] != null)
                nameFields[i].text = playerNames[i];
            else
                nameFields[i].text = "Empty";
        }

        //show joincode to host
        joinCodeText.text = "Join Code: " + ConnectionManager.RelayJoinCode;
    }

    private void UpdatePlayerNames(string[] names)
    {
        playerNames.Clear();
        foreach (string str in names)
            playerNames.Add(str);
    }
}
