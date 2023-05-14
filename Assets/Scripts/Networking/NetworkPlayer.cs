using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class NetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<ulong> playerId = new NetworkVariable<ulong>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> levelLoaded = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> assignedTeam = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString64Bytes> currentCharacter = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public GameObject character;
    public PlayerController controller;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if(controller != null)
        {
            if (controller.Team != assignedTeam.Value)
                controller.SetTeamServerRpc(assignedTeam.Value);
        }
    }

    public void SetId(ulong _id)
    {
        if (IsOwner)
            playerId.Value = _id;
    }

    public void SetName(string _name)
    {
        if(IsOwner)
            playerName.Value = _name;
    }

    public void StartLoadLevel()
    {
        if(IsOwner)
            levelLoaded.Value = false;
    }

    public void LevelLoadedSuccessfully()
    {
        if(IsOwner)
            levelLoaded.Value = true;
    }

    public void Disconnect()
    {
        if (!IsOwner) return;

        if(!IsHost) 
            DisconnectClientServerRpc(OwnerClientId);
        
        NetworkManager.Singleton.Shutdown();
    }

    [ServerRpc]
    private void DisconnectClientServerRpc(ulong clientId)
    {
        //NetworkManager.DisconnectClient(clientId);
        ConnectionManager.Instance.OnClientDisconnectedListener(clientId);
    }

    public void RequestLoadCharacter(string characterName)
    {
        if (!IsOwner || (character != null && !character.name.Contains(characterName))) return;

        LoadCharacterServerRpc(NetworkManager.Singleton.LocalClientId, new FixedString64Bytes(characterName));
    }

    private void AssignCharacter(GameObject newChar)
    {
        character = newChar;
        controller = character.GetComponent<PlayerController>();
        currentCharacter.Value = new FixedString64Bytes(controller.characterName);
        GameManager.Instance.RequestSceneChange("InGame");
    }

    [ServerRpc]
    private void LoadCharacterServerRpc(ulong clientId, FixedString64Bytes character)
    {
        GameObject obj = Instantiate(Database.LoadCharacter(character.ToString()));
        obj.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
        AssignCharacterClientRpc(clientId, obj.GetComponent<NetworkObject>().NetworkObjectId);
    }

    [ClientRpc]
    private void AssignCharacterClientRpc(ulong targetClient, ulong objectId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClient) return;

        AssignCharacter(GetNetworkObject(objectId).gameObject);
    }

    [ClientRpc]
    public void AssignTeamClientRpc(ulong targetClient, ulong playerNetPlayerId, int team)
    {

        if (NetworkManager.Singleton.LocalClientId == targetClient)
        {
            assignedTeam.Value = team;
            GameManager.Instance.SetPlayerToTeam(this, team);
        }
        else
            GameManager.Instance.SetPlayerToTeam(GetNetworkObject(playerNetPlayerId).GetComponent<NetworkPlayer>(), team);
    }
}
