using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class RelayMatchmaking : MonoBehaviour
{
    private UnityTransport _transport;
    public GameObject _buttons;
    public int MaxPlayers = 2;
    private bool AlreadyHosted = false;
    public TextMeshProUGUI _joinText;
    private string lobbyJoinText;
    public GameObject MessageBoxGmj;
    private Coroutine DialogCoroutine;
    public GameObject JoinWaitingGameObject;

    [Header("HOSTING")]
    public GameObject HostingMenuGameObject;
    public GameObject JoinCodeGameObject;
    public GameObject MultiplayerMenuGameObject;
    public GameObject NameInputGameObject;
    public GameObject MainMenuGameObject;
    public GameObject LoadinServerGameObject;
    public GameObject ReconnectButton;

    private async void Awake()
    {
        _transport = FindObjectOfType<UnityTransport>();

        await Authenticate();

    }

    public async void InitialiseButton()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log("RECONNECT SUCCESFUL");
            ShowMessage("Reconnection Successful!");
        }
        catch(Exception ex)
        {
            ShowMessage("Reconnection Failed!");
        }
    }

    private IEnumerator MessageBox(string Message)
    {
        MessageBoxGmj.SetActive(true);
        TextMeshProUGUI t = MessageBoxGmj.GetComponentInChildren<TextMeshProUGUI>();
        t.text = Message;
        yield return new WaitForSeconds(2f);
        MessageBoxGmj.SetActive(false);
    }

    private void ShowMessage(string message)
    {
        if (DialogCoroutine != null)
        {
            StopCoroutine(DialogCoroutine);
            DialogCoroutine = null;
            DialogCoroutine = StartCoroutine(MessageBox(message));
        }
        else
        {
            DialogCoroutine = StartCoroutine(MessageBox(message));
        }
    }

    private static async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateGame()
    {
        MyNetworkManager.instance.PlayerName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        Debug.LogError("Player's name: " + MyNetworkManager.instance.PlayerName);
        //_buttons.SetActive(false);
        if (AlreadyHosted)
        {
            ShowMessage("A Server instance is already running!");
            return;
        }

        try
        {
            MultiplayerMenuGameObject.SetActive(false);
            NameInputGameObject.SetActive(false);
            LoadinServerGameObject.SetActive(true);
            Allocation a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            _joinText.text = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);


            _transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

            NetworkManager.Singleton.StartHost();
            AlreadyHosted = true;
            PlayerNetworkManager.InitialisePlayerDataHost();
            //Creation was successful
            LoadinServerGameObject.SetActive(false);
            HostingMenuGameObject.SetActive(true);
            MultiplayerMenuGameObject.SetActive(false);
            JoinCodeGameObject.SetActive(true);
            NameInputGameObject.SetActive(false);
            ReconnectButton.SetActive(false);
        }
        catch (Exception ex)
        {
            AudioManager.instance.ButtonFailed();
            LoadinServerGameObject.SetActive(false);
            Debug.LogError(ex.Message);
            ShowMessage("Please try to reconnect with a working network connection!");
            MultiplayerMenuGameObject.SetActive(false);
            NameInputGameObject.SetActive(false);
            MainMenuGameObject.SetActive(true);
        }
    }
    public async void JoinGame()
    {
        GameObject NameInput = GameObject.Find("NameInput");
        MyNetworkManager.instance.PlayerName = NameInput.GetComponent<InputField>().text;
        Debug.LogError("Player's name: " + MyNetworkManager.instance.PlayerName);
        _buttons.SetActive(false);

        lobbyJoinText = GameObject.Find("InputLobby").GetComponent<InputField>().text;

        try
        {
            JoinAllocation a = await RelayService.Instance.JoinAllocationAsync(lobbyJoinText);


            _transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
            NetworkManager.Singleton.StartClient();
            NameInputGameObject.SetActive(false);
            NameInput.SetActive(false);
            JoinWaitingGameObject.SetActive(true);
        }
        catch (Exception e)
        {
            AudioManager.instance.ButtonFailed();
            Debug.LogError($"Error joining game: {e.Message}");
            ShowMessage("Invalid lobby ID entered.");
            _buttons.SetActive(true);
        }
    }
}
