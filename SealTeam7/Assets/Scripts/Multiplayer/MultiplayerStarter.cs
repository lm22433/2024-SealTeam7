using FishNet;
using FishNet.Transporting;
using FishNet.Transporting.Tugboat;
using UnityEditor;
using UnityEngine;
using Unity.Multiplayer;
 

public class MultiplayerStarter : MonoBehaviour
{
    
    [SerializeField] private Tugboat _tugboat;
    
    private void Awake()
    {

        switch (MultiplayerRolesManager.ActiveMultiplayerRoleMask)
        {
            case MultiplayerRoleFlags.Server:
                Debug.Log("Server Started");
                break;
            case MultiplayerRoleFlags.Client:
                Debug.Log("Client Connected");
                _tugboat.StartConnection(true);
                break;

        }
    }
}
