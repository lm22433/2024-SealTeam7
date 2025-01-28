using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using UnityEditor;
using UnityEngine;
using Unity.Multiplayer;
using FishNet.Managing;
using System.Collections;

public class MultiplayerStarter : MonoBehaviour {
    
    [SerializeField] private NetworkManager _networkManager;
    bool isServer = false;
    
    private void Start()
    {

        switch (MultiplayerRolesManager.ActiveMultiplayerRoleMask)
        {
            case MultiplayerRoleFlags.Server:
                Debug.Log("Server Started");
                isServer = true;
                StartServer();
                break;
            case MultiplayerRoleFlags.Client:
                Debug.Log("Client Connecting");
                ConnectClient();
                break;

        }
    }

    private void StartServer() {
        if (_networkManager == null)
            return;
        if (!_networkManager.ServerManager.Started) {
            _networkManager.ServerManager.StartConnection();
        }
    }

    private void ConnectClient()
    {
        if (_networkManager == null)
            return;

        if (_networkManager.ClientManager.StartConnection()) {
            Debug.Log("Client Sucessfully Connected");
        }
    }
}
