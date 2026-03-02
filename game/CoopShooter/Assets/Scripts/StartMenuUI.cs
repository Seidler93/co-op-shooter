using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField addressField;
    [SerializeField] private TMP_InputField portField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private UnityTransport Transport => NetworkManager.Singleton.GetComponent<UnityTransport>();

    public void Host()
    {
        if (NetworkManager.Singleton.IsListening) return;

        ApplyConnectionData();
        SetButtons(false);

        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log("StartHost() returned: " + ok);

        if (!ok)
        {
            SetButtons(true);
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void Join()
    {
        if (NetworkManager.Singleton.IsListening) return;

        ApplyConnectionData();
        SetButtons(false);

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log("StartClient() returned: " + ok);

        if (!ok)
        {
            SetButtons(true);
        }
    }

    private void SetButtons(bool enabled)
    {
        if (hostButton) hostButton.interactable = enabled;
        if (joinButton) joinButton.interactable = enabled;
    }

    private void ApplyConnectionData()
    {
        var addr = string.IsNullOrWhiteSpace(addressField?.text) ? "127.0.0.1" : addressField.text.Trim();
        var portText = string.IsNullOrWhiteSpace(portField?.text) ? "7777" : portField.text.Trim();

        ushort port = 7777;
        ushort.TryParse(portText, out port);

        Transport.ConnectionData.Address = addr;
        Transport.ConnectionData.Port = port;

        Debug.Log($"ConnectionData set: {addr}:{port}");
    }
}