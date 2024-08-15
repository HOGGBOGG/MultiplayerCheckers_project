using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Text;

public class PlayerNetworkManager : MonoBehaviour
{
    private static Dictionary<ulong, PlayerData> clientData = new Dictionary<ulong, PlayerData>();

    public static void InitialisePlayerDataHost()
    {
        clientData[NetworkManager.Singleton.LocalClientId] = new PlayerData(MyNetworkManager.instance.PlayerName);
        Debug.LogError("INITIALISED HOST DATA: " + clientData[NetworkManager.Singleton.LocalClientId].PlayerName);
    }

    public static void InitialisePlayerDataClient(byte[] connectionData,ulong playerID)
    {
        string payload = Encoding.ASCII.GetString(connectionData);
        clientData[playerID] = new PlayerData(payload);
        Debug.LogError("INITIALISED CLIENT DATA: " + clientData[playerID].PlayerName);
    }

    public static PlayerData? GetPlayerData(ulong playerID)
    {
        if(clientData.TryGetValue(playerID, out PlayerData playerData))
        {
            return playerData;
        }
        return null;
    }
}
