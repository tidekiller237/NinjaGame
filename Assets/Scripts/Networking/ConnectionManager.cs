using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
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

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;

    const int MaxConnections = 4;
    public static string RelayJoinCode;
    public static string AuthPlayerID;

    public static bool IsHost { get { return NetworkManager.Singleton.IsHost; } }
    public static bool IsConnected { get { return NetworkManager.Singleton.IsConnectedClient; } }

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

    #endregion
}
