using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; set; }

    public void HotseatButton()
    {
        SceneManager.LoadScene("Singleplayer");
    }

    public void QuitGame()
    {
        Debug.Log("QuitGame has been called");
        Application.Quit();
    }

    public void ToMainMenuButton()
    {
        SceneManager.LoadScene("Lobby");
    }
}
 