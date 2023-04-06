using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class NetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName;

    private void Awake()
    {
        playerName = new NetworkVariable<FixedString64Bytes>();
    }

    public void SetName(string _name)
    {
        if (IsServer)
        {
            playerName.Value = new FixedString64Bytes(_name);
        }
    }
}
