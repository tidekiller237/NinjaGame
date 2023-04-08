using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class UI_LevelMenuManager : MonoBehaviour
{
    public List<Button> characterButtons;

    private void Start()
    {
        foreach(Button button in characterButtons)
        {
            button.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().RequestLoadCharacter(button.GetComponent<UIComp_CharacterButton>().characterName);
            });
        }
    }
}