using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Rematch : MonoBehaviour
{
    public GameObject BGM;

    private void OnEnable()
    {
        //StartCoroutine(DisableBGM());
    }

    //private IEnumerator DisableBGM()
    //{
    //    yield return new WaitForSeconds(3f);
    //    var arr = GameObject.FindGameObjectsWithTag("BGM");
    //    arr[0].gameObject.SetActive(false);
    //}
    public void QuitToMainMenuButton()
    {
        NetworkManager.Singleton.Shutdown();
        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }
    public void QuitGame()
    {
        Debug.LogError("Quitgame called");
        Application.Quit();
    }
}
