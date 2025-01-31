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
    
    private void Start()
    {

        switch (MultiplayerRolesManager.ActiveMultiplayerRoleMask)
        {
            case MultiplayerRoleFlags.Server:
                Debug.Log("Server Started");
                StartServer();
                break;
            case MultiplayerRoleFlags.Client:
                Debug.Log("Client Connecting");
                ConnectClient();
                break;
            case MultiplayerRoleFlags.ClientAndServer:
                StartServer();
                StartCoroutine(WaitToConnectClient());
                break;

        }
    }

    private IEnumerator WaitToConnectClient() {
        yield return new WaitForSeconds(5);

        ConnectClient();
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
