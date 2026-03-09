using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField addressField;
    [SerializeField] private TMP_InputField portField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private UnityTransport Transport
    {
        get
        {
            if (NetworkManager.Singleton == null) return null;
            return NetworkManager.Singleton.GetComponent<UnityTransport>();
        }
    }

    public void Host()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[StartMenuUI] No NetworkManager.Singleton found.");
            return;
        }

        if (Transport == null)
        {
            Debug.LogError("[StartMenuUI] No UnityTransport found on NetworkManager.");
            return;
        }

        if (NetworkManager.Singleton.IsListening) return;

        ApplyConnectionData();
        SetButtonsInteractable(false);

        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log("[StartMenuUI] StartHost() returned: " + ok);

        if (!ok)
        {
            SetButtonsInteractable(true);
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void Join()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[StartMenuUI] No NetworkManager.Singleton found.");
            return;
        }

        if (Transport == null)
        {
            Debug.LogError("[StartMenuUI] No UnityTransport found on NetworkManager.");
            return;
        }

        if (NetworkManager.Singleton.IsListening) return;

        ApplyConnectionData();
        SetButtonsInteractable(false);

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log("[StartMenuUI] StartClient() returned: " + ok);

        if (!ok)
        {
            SetButtonsInteractable(true);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (hostButton) hostButton.interactable = interactable;
        if (joinButton) joinButton.interactable = interactable;
    }

    private void ApplyConnectionData()
    {
        var addr = string.IsNullOrWhiteSpace(addressField?.text) ? "127.0.0.1" : addressField.text.Trim();
        var portText = string.IsNullOrWhiteSpace(portField?.text) ? "7777" : portField.text.Trim();

        ushort port = 7777;
        ushort.TryParse(portText, out port);

        Transport.ConnectionData.Address = addr;
        Transport.ConnectionData.Port = port;

        Debug.Log($"[StartMenuUI] ConnectionData set: {addr}:{port}");
    }
}