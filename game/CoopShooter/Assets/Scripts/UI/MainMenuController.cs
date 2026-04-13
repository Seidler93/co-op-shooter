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

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject onlinePanel;
    [SerializeField] private GameObject barracksPanel;
    [SerializeField] private GameObject challengesPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private FriendsPanelUI friendsPanel;

    [Header("Play")]
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text playButtonLabel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text onlineStatusText;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private TMP_Text profileNameText;
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private PartyPanelUI partyPanelUI;
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private bool showConnectionInfo;

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

    protected virtual void Start()
    {
        ShowMainPanel();
    }

    protected virtual void OnEnable()
    {
        if (playButton != null)
            playButton.onClick.AddListener(Play);

        RegisterNetworkCallbacks();
        RefreshPlayUI();
    }

    protected virtual void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(Play);

        UnregisterNetworkCallbacks();
    }

    public void ShowMainPanel()
    {
        HideAllPanels();
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void ShowOnlinePanel()
    {
        HideAllPanels();
        if (onlinePanel != null) onlinePanel.SetActive(true);
    }

    public void ToggleOnlinePanel()
    {
        if (onlinePanel == null) return;

        bool isActive = onlinePanel.gameObject.activeSelf;
        HideAllPanels();
        onlinePanel.gameObject.SetActive(!isActive);
    }

    public void ShowBarracksPanel()
    {
        HideAllPanels();
        if (barracksPanel != null) barracksPanel.SetActive(true);
    }

    public void ShowChallengesPanel()
    {
        HideAllPanels();
        if (challengesPanel != null) challengesPanel.SetActive(true);
    }

    public void ShowSettingsPanel()
    {
        HideAllPanels();
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void ShowFriendsPanel()
    {
        HideAllPanels();
        friendsPanel?.OpenFriends();
    }

    public void ShowInviteFriendsPanel()
    {
        HideAllPanels();
        friendsPanel?.OpenInvitePicker();
    }

    public void ShowNotificationsPanel()
    {
        HideAllPanels();
        friendsPanel?.OpenNotifications();
    }

    public void ShowAddFriendPanel()
    {
        HideAllPanels();
        friendsPanel?.OpenAddFriend();
    }

    public async void Play()
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

    public async Task<string> GetOrCreateInviteJoinCodeAsync()
    {
        if (!string.IsNullOrWhiteSpace(currentJoinCode))
            return currentJoinCode;

        if (isBusy)
        {
            SetStatus("Please wait...");
            return string.Empty;
        }

        if (!HasNetworkReferences())
            return string.Empty;

        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            SetStatus("You are already in someone else's party.");
            return currentJoinCode;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            await HostPartyAsync();
        }

        return currentJoinCode;
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
            Debug.LogError($"[MainMenuController] Join invite failed: {ex}");
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
            RefreshPlayUI();
            return;
        }

        NetworkManager.Singleton.Shutdown();
        currentJoinCode = string.Empty;
        partyPanelUI?.SetLocalMemberStatus(string.Empty);
        SetStatus("Left party.");
        RefreshPlayUI();
    }

    public void StartGameForParty()
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

    public async Task HostPartyAsync()
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
            Debug.LogError($"[MainMenuController] Host failed: {ex}");
            SetStatus($"Could not start party: {ex.Message}");
            currentJoinCode = string.Empty;
        }
        finally
        {
            SetBusy(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HideAllPanels()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (onlinePanel != null) onlinePanel.SetActive(false);
        if (barracksPanel != null) barracksPanel.SetActive(false);
        if (challengesPanel != null) challengesPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        friendsPanel?.Hide();
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
        RefreshPlayUI();
    }

    private void HandleClientConnected(ulong clientId)
    {
        RefreshPlayUI();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        RefreshPlayUI();
    }

    private void SetBusy(bool busy, string message = null)
    {
        isBusy = busy;

        if (!string.IsNullOrWhiteSpace(message))
        {
            SetStatus(message);
        }

        RefreshPlayUI();
    }

    private void SetStatus(string message)
    {
        Debug.Log($"[MainMenuController] {message}");

        if (statusText != null)
            statusText.text = message;
    }

    private void RefreshPlayUI()
    {
        if (playButton != null)
            playButton.interactable = !isBusy;

        if (playButtonLabel != null)
            playButtonLabel.text = GetPlayLabel();

        if (onlineStatusText != null)
        {
            onlineStatusText.gameObject.SetActive(showConnectionInfo);
            onlineStatusText.text = GetOnlineStatus();
        }

        if (versionText != null)
            versionText.text = $"v{Application.version}";

        if (profileNameText != null)
            profileNameText.text = GetProfileName();

        if (playerLevelText != null)
            playerLevelText.text = $"LVL {Mathf.Max(1, playerLevel)}";
    }

    private string GetProfileName()
    {
        FriendsService service = FriendsService.Instance;
        if (service == null)
            return "Pilot";

        if (!string.IsNullOrWhiteSpace(service.CurrentDisplayName))
            return service.CurrentDisplayName;

        if (!string.IsNullOrWhiteSpace(service.CurrentUsername))
            return service.CurrentUsername;

        return "Player";
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
