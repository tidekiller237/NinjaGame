using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LandingScene : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
