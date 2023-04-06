using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Http;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;
using Unity.Collections;

public class ConnectionManager : NetworkBehaviour
{
    public static ConnectionManager Instance;

    const int MaxConnections = 4;
    public static string RelayJoinCode;
    public static string AuthPlayerID;

    public static bool IsHost { get { return NetworkManager.Singleton.IsHost; } }
    public static bool IsConnected { get { return NetworkManager.Singleton.IsConnectedClient; } }

    public static List<NetworkPlayer> connectedPlayers;
    public static UnityEvent<string[]> onPlayerUdate;

    private void Awake()
    {
        if (ConnectionManager.Instance == null)
        {
            ConnectionManager.Instance = this;
            Initialize();
        }
    }

    public async void Initialize()
    {
        //run initialization
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"UnityServices Initialization failed: {e.Message}");
            return;
        }

        AuthenticatePlayer();

        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedListener;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedListener;

        connectedPlayers = new List<NetworkPlayer>();
        onPlayerUdate = new UnityEvent<string[]>();
    }

    async void AuthenticatePlayer()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            AuthPlayerID = AuthenticationService.Instance.PlayerId;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Authentication Anonymous sign-in failed: {e.Message}");
            return;
        }
    }

    public void CancelConnection()
    {
        StopAllCoroutines();
        //close relay allocation
    }

    public void OnClientConnectedListener(ulong clientId)
    {
        //update players
        UpdatePlayersServerRpc();
    }

    public void OnClientDisconnectedListener(ulong clientId)
    {
        //update players
        UpdatePlayersServerRpc();
    }

    private void UpdatePlayers()
    {
        FixedString64Bytes[] names = new FixedString64Bytes[8];

        connectedPlayers.Clear();

        foreach(var player in NetworkManager.Singleton.ConnectedClientsList)
        {
            connectedPlayers.Add(player.PlayerObject.GetComponent<NetworkPlayer>());
        }
        for(int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
        {
            connectedPlayers.Add(NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<NetworkPlayer>());

            if (i < 8)
                names[i] = NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<NetworkPlayer>().playerName.Value;
        }

        UpdatePlayerNamesClientRpc(
            names[0],
            names[1],
            names[2],
            names[3],
            names[4],
            names[5],
            names[6],
            names[7]
            );
    }

    [ServerRpc]
    private void UpdatePlayersServerRpc()
    {
        UpdatePlayers();
    }

    [ClientRpc]
    private void UpdatePlayerNamesClientRpc(
        FixedString64Bytes l1, 
        FixedString64Bytes l2, 
        FixedString64Bytes l3, 
        FixedString64Bytes l4, 
        FixedString64Bytes l5, 
        FixedString64Bytes l6, 
        FixedString64Bytes l7, 
        FixedString64Bytes l8)
    {
        string[] newList = new string[8];
        newList[0] = l1.ToString();
        newList[1] = l2.ToString();
        newList[2] = l3.ToString();
        newList[3] = l4.ToString();
        newList[4] = l5.ToString();
        newList[5] = l6.ToString();
        newList[6] = l8.ToString();

        onPlayerUdate.Invoke(newList);
    }

    #region Host

    public void InitializeHost()
    {
        StopAllCoroutines();
        StartCoroutine(InitializeHostCoroutine());
    }

    private IEnumerator InitializeHostCoroutine()
    {
        var relayServerDataTask = AllocateRelay();

        while(!relayServerDataTask.IsCompleted)
            yield return null;

        if (relayServerDataTask.IsFaulted)
        {
            Debug.LogError($"Exception thrown when attempting to start Relay Server. Server not started. Excpetion: {relayServerDataTask.Exception.Message}");
            yield break;
        }

        RelayServerData relayServerData = relayServerDataTask.Result;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();

        yield return null;
    }

    public static async Task<RelayServerData> AllocateRelay()
    {
        Allocation allocation;

        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation request failed: {e.Message}");
            throw;
        }

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        try
        {
            RelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch (Exception e)
        {
            Debug.Log($"Relay get join code failed: {e.Message}");
            throw;
        }

        return new RelayServerData(allocation, "dtls");
    }

    #endregion

    #region Client

    public void InitializeClient(string joinCode)
    {
        StopAllCoroutines();
        StartCoroutine(InitializeClientCoroutine(joinCode));
    }

    private IEnumerator InitializeClientCoroutine(string joinCode)
    {
        var relayServerDataTask = JoinRelayServer(joinCode);
        
        while(!relayServerDataTask.IsCompleted)
            yield return null;

        if (relayServerDataTask.IsFaulted)
        {
            Debug.LogError($"Exception thrown when attempting to connect to relay server. Exception: {relayServerDataTask.Exception}");
            yield break;
        }

        RelayServerData relayServerData = relayServerDataTask.Result;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartClient();

        yield return null;
    }

    public static async Task<RelayServerData> JoinRelayServer(string joinCode)
    {
        JoinAllocation allocation;

        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay join allocation failed: {e.Message}");
            throw;
        }

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        return new RelayServerData(allocation, "dtls");
    }

    public void SetPlayerName(string playerName)
    {
        SetPlayerNameServerRpc(OwnerClientId, playerName);
    }

    [ServerRpc]
    public void SetPlayerNameServerRpc(ulong clientId, string playerName)
    {
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkPlayer>().SetName(playerName);
        UpdatePlayersServerRpc();
    }

    #endregion
}
