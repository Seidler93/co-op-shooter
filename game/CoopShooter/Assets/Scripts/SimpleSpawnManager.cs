using Unity.Netcode;
using UnityEngine;

public class SimpleSpawnManager : MonoBehaviour
{
    private void Awake()
    {
        RegisterApprovalCallback();
    }

    private void OnEnable()
    {
        RegisterApprovalCallback();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectionApprovalCallback == ApprovalCheck)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
        }
    }

    private void RegisterApprovalCallback()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                               NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
    }
}
