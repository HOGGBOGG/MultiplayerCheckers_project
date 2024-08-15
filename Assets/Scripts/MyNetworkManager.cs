using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    public string PlayerName;
    public GameObject HostMenu;
    public GameObject StartButton;
    public TextMeshProUGUI ConnectedText;
    public bool gameStarted = false;
    public int connectedClients = 0;
    public static MyNetworkManager instance {  get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            GetComponent<NetworkManager>().SetSingleton();
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
        //instance = this;
        HostMenu = GameObject.Find("Host_Menu");
        HostMenu.SetActive(true);
        OnConnectionEvent += ClientConnected;
        ConnectedText = GameObject.Find("PlayerConnectedText").GetComponent<TextMeshProUGUI>();
        //StartButton = GameObject.Find("Start_b(Host)");
        //StartButton.SetActive(false);
        HostMenu.SetActive(false);
    }

    public void ClientConnected(NetworkManager e,ConnectionEventData d)
    {
        connectedClients++;
        int clamped = Mathf.Clamp(connectedClients, 1,2);
        ConnectedText.text = "Players in lobby: " + clamped.ToString();
        if(connectedClients == 2)
        {
            //StartButton.SetActive(true);
            //SceneManager.LoadScene("Lobby",LoadSceneMode.Single);
            SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        Debug.LogError("Connected clients: " +  connectedClients);
    }
}
