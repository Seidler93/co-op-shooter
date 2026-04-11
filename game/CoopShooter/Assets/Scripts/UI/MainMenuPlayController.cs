using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuPlayController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text playButtonLabel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text onlineStatusText;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private PartyPanelUI partyPanelUI;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Relay")]
    [Tooltip("Remote clients allowed in the party. For host + 1 friend, use 1.")]
    [SerializeField] private int maxClientConnections = 1;
    [SerializeField] private string connectionType = "dtls";

    [Header("Flow")]
    [Tooltip("If true, pressing Play while offline creates a Relay party and immediately loads gameplay.")]
    [SerializeField] private bool autoStartAfterHosting = true;

    private bool isBusy;
    private string currentJoinCode = string.Empty;

    public bool IsBusy => isBusy;
    public string CurrentJoinCode => currentJoinCode;

    private UnityTransport Transport
    {
        get
        {
            if (NetworkManager.Singleton == null) return null;
            return NetworkManager.Singleton.GetComponent<UnityTransport>();
        }
    }

    private void OnEnable()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        RegisterNetworkCallbacks();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        UnregisterNetworkCallbacks();
    }

    private async void OnPlayClicked()
    {
        await PlayAsync();
    }

    public async Task PlayAsync()
    {
        if (isBusy)
        {
            SetStatus("Please wait...");
            return;
        }

        if (!HasNetworkReferences())
            return;

        if (NetworkManager.Singleton.IsHost)
        {
            StartGameForParty();
            return;
        }

        if (NetworkManager.Singleton.IsClient)
        {
            RequestHostStartGame();
            return;
        }

        await HostPartyAsync();

        if (autoStartAfterHosting)
        {
            StartGameForParty();
        }
    }

    public async Task JoinInvitedPartyAsync(string relayJoinCode)
    {
        if (string.IsNullOrWhiteSpace(relayJoinCode))
        {
            SetStatus("Invite is missing party data.");
            return;
        }

        if (!HasNetworkReferences())
            return;

        if (NetworkManager.Singleton.IsListening)
        {
            SetStatus("Already in a party.");
            return;
        }

        partyPanelUI?.SetLocalMemberStatus("Joining...");
        SetBusy(true, "Joining party...");

        try
        {
            await EnsureServicesReadyAsync();

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode.Trim().ToUpperInvariant());
            Transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

            if (!NetworkManager.Singleton.StartClient())
            {
                throw new Exception("NetworkManager.StartClient() returned false.");
            }

            currentJoinCode = relayJoinCode.Trim().ToUpperInvariant();
            partyPanelUI?.SetLocalMemberStatus(string.Empty);
            SetStatus("Joined party. Ready to play.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MainMenuPlayController] Join invite failed: {ex}");
            SetStatus($"Could not join party: {ex.Message}");
            currentJoinCode = string.Empty;
            partyPanelUI?.SetLocalMemberStatus(string.Empty);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public void DisconnectParty()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            SetStatus("Not in a party.");
            RefreshUI();
            return;
        }

        NetworkManager.Singleton.Shutdown();
        currentJoinCode = string.Empty;
        partyPanelUI?.SetLocalMemberStatus(string.Empty);
        SetStatus("Left party.");
        RefreshUI();
    }

    private async Task HostPartyAsync()
    {
        SetBusy(true, "Creating party...");

        try
        {
            await EnsureServicesReadyAsync();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxClientConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

            if (!NetworkManager.Singleton.StartHost())
            {
                throw new Exception("NetworkManager.StartHost() returned false.");
            }

            currentJoinCode = joinCode;
            SetStatus("Party ready.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MainMenuPlayController] Host failed: {ex}");
            SetStatus($"Could not start party: {ex.Message}");
            currentJoinCode = string.Empty;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void StartGameForParty()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("NetworkManager missing.");
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            RequestHostStartGame();
            return;
        }

        SetStatus("Starting game...");
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    private void RequestHostStartGame()
    {
        if (PartyManager.Instance == null)
        {
            SetStatus("Waiting for party sync...");
            return;
        }

        SetStatus("Requesting game start...");
        PartyManager.Instance.RequestStartGame();
    }

    private async Task EnsureServicesReadyAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized &&
            UnityServices.State != ServicesInitializationState.Initializing)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private bool HasNetworkReferences()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("NetworkManager missing.");
            return false;
        }

        if (Transport == null)
        {
            SetStatus("UnityTransport missing.");
            return false;
        }

        if (NetworkManager.Singleton.ShutdownInProgress)
        {
            SetStatus("Network is shutting down...");
            return false;
        }

        return true;
    }

    private void RegisterNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted += HandleNetworkChanged;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void UnregisterNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted -= HandleNetworkChanged;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void HandleNetworkChanged()
    {
        RefreshUI();
    }

    private void HandleClientConnected(ulong clientId)
    {
        RefreshUI();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        RefreshUI();
    }

    private void SetBusy(bool busy, string message = null)
    {
        isBusy = busy;

        if (!string.IsNullOrWhiteSpace(message))
        {
            SetStatus(message);
        }

        RefreshUI();
    }

    private void SetStatus(string message)
    {
        Debug.Log($"[MainMenuPlayController] {message}");

        if (statusText != null)
            statusText.text = message;
    }

    private void RefreshUI()
    {
        if (playButton != null)
            playButton.interactable = !isBusy;

        if (playButtonLabel != null)
            playButtonLabel.text = GetPlayLabel();

        if (onlineStatusText != null)
            onlineStatusText.text = GetOnlineStatus();

        if (versionText != null)
            versionText.text = $"v{Application.version}";
    }

    private string GetPlayLabel()
    {
        if (isBusy) return "WORKING...";
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return "PLAY";
        if (NetworkManager.Singleton.IsHost) return "START GAME";
        return "READY";
    }

    private string GetOnlineStatus()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return "Offline";
        if (NetworkManager.Singleton.IsHost) return "Hosting party";
        return "In party";
    }
}
