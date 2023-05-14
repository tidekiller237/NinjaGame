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

    const int MaxConnections = 8;
    public static string RelayJoinCode;
    public static string AuthPlayerID;

    public static bool IsHost { get { return NetworkManager.Singleton.IsHost; } }
    public static bool IsConnectedClient { get { return NetworkManager.Singleton.IsConnectedClient; } }

    public NetworkVariable<bool> waitingForPlayers;
    public NetworkList<ulong> connectedPlayersIds;
    public NetworkList<FixedString64Bytes> connectedPlayersNames;

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

        waitingForPlayers = new NetworkVariable<bool>();
        connectedPlayersIds = new NetworkList<ulong>();
        connectedPlayersNames = new NetworkList<FixedString64Bytes>();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedListener;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedListener;
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
        NetworkManager.Singleton.Shutdown();
        //close relay allocation
    }

    public void OnClientConnectedListener(ulong clientId)
    {
        if (!IsServer) return;
        if(clientId == NetworkManager.Singleton.LocalClientId)
        {
            connectedPlayersIds.Clear();
            connectedPlayersNames.Clear();

            for(int i = 0; i < 8; i++)
            {
                connectedPlayersIds.Add(ulong.MaxValue);
                connectedPlayersNames.Add("");
            }
        }

        AddPlayer(clientId);
        EvaluateSceneState(clientId);
        UpdateTeams();
    }

    public void OnClientDisconnectedListener(ulong clientId)
    {
        if(!IsServer) return;
        RemovePlayer(clientId);
        UpdateTeams();
    }

    private void EvaluateSceneState(ulong clientId)
    {
        GameManager.SceneState currState = GameManager.Instance.sceneState;

        if(currState != GameManager.SceneState.MainMenu && currState != GameManager.SceneState.Lobby)
        {
            BeginLevelTransitionClientRpc(clientId, new FixedString64Bytes(GameManager.Instance.currentLevel));
        }
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

    public void SetPlayerIdAndName(ulong playerId, string playerName)
    {
        //set player id
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().SetId(playerId);

        //set player name
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().SetName(playerName);
    }

    #endregion

    #region Lobby

    private void AddPlayer(ulong clientId)
    {
        if (connectedPlayersIds.Contains(clientId)) return;

        int index = connectedPlayersIds.IndexOf(ulong.MaxValue);
        connectedPlayersIds[index] = clientId;
        connectedPlayersNames[index] = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkPlayer>().playerName.Value;
        //connectedPlayersIds.Add(clientId);
        //connectedPlayersNames.Add(NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkPlayer>().playerName.Value);
        NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkPlayer>().playerName.OnValueChanged += UpdatePlayerName;
    }

    private void RemovePlayer(ulong clientId)
    {
        if (!connectedPlayersIds.Contains(clientId)) return;

        int index = connectedPlayersIds.IndexOf(clientId);
        connectedPlayersIds[index] = ulong.MaxValue;
        connectedPlayersNames[index] = "";
        //connectedPlayersIds.RemoveAt(index);
        //connectedPlayersIds.Insert(index, ulong.MaxValue);
        //connectedPlayersNames.RemoveAt(index);
    }

    private void UpdatePlayerName(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        int index = connectedPlayersNames.IndexOf(oldValue);
        connectedPlayersNames[index] = newValue;
    }

    #endregion

    #region Scene

    public void RequestNetworkSceneChange(string sceneName)
    {
        if (!IsServer) return;
        BeginLevelTransitionClientRpc(new FixedString64Bytes(sceneName));
        StartLevel();
    }

    [ClientRpc]
    private void BeginLevelTransitionClientRpc(FixedString64Bytes sceneName)
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().StartLoadLevel();
        GameManager.Instance.RequestSceneChange(sceneName.ToString());
    }

    [ClientRpc]
    private void BeginLevelTransitionClientRpc(ulong targetClient, FixedString64Bytes sceneName)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClient) return;

        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().StartLoadLevel();
        GameManager.Instance.RequestSceneChange(sceneName.ToString());
    }

    public void StartLevel()
    {
        if (!IsServer) return;

        StartCoroutine(WaitForPlayers());
    }

    private IEnumerator WaitForPlayers()
    {
        waitingForPlayers.Value = true;
        bool check = false;

        while (!check)
        {
            yield return null;
            check = true;

            for(int i = 0; i < connectedPlayersIds.Count; i++)
            {
                if (connectedPlayersIds[i] == ulong.MaxValue) continue;

                if (!NetworkManager.ConnectedClients[connectedPlayersIds[i]].PlayerObject.GetComponent<NetworkPlayer>().levelLoaded.Value)
                    check = false;
            }
        }

        waitingForPlayers.Value = false;
    }

    #endregion

    #region Teams

    public void UpdateTeams()
    {
        for(int i = 0; i < connectedPlayersIds.Count; i++)
        {
            if (connectedPlayersIds[i] < ulong.MaxValue)
            {
                ulong targetClient = connectedPlayersIds[i];
                ulong playerNetworkPlayerId = NetworkManager.ConnectedClients[targetClient].PlayerObject.NetworkObjectId;

                if (i % 2 == 0)
                {
                    NetworkManager.ConnectedClients[connectedPlayersIds[i]].PlayerObject.GetComponent<NetworkPlayer>().AssignTeamClientRpc(targetClient, playerNetworkPlayerId, 1);
                }
                else
                {
                    NetworkManager.ConnectedClients[connectedPlayersIds[i]].PlayerObject.GetComponent<NetworkPlayer>().AssignTeamClientRpc(targetClient, playerNetworkPlayerId, 2);
                }
            }
        }
    }

    #endregion
}
