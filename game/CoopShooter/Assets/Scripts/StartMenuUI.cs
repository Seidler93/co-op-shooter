using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class StartMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button disconnectButton;

    [Header("Scenes")]
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Relay")]
    [Tooltip("For host + 1 remote player, use 1.")]
    [SerializeField] private int maxClientConnections = 1;

    [Tooltip("Use dtls for normal desktop builds.")]
    [SerializeField] private string connectionType = "dtls";

    private bool isBusy;
    private string currentJoinCode = string.Empty;

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
        RegisterCallbacks();
        RefreshUI();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (!string.IsNullOrWhiteSpace(currentJoinCode))
                SetJoinCode(currentJoinCode);

            SetStatus(NetworkManager.Singleton.IsHost
                ? $"Host running. Code: {currentJoinCode}"
                : "Connected.");
        }
        else
        {
            SetStatus("Ready.");
            ClearJoinCode();
        }
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(Disconnect);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    private void UnregisterCallbacks()
    {
        if (hostButton != null) hostButton.onClick.RemoveListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.RemoveListener(OnJoinClicked);
        if (disconnectButton != null) disconnectButton.onClick.RemoveListener(Disconnect);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private async void OnHostClicked()
    {
        await HostOnlineAsync();
    }

    private async void OnJoinClicked()
    {
        await JoinOnlineAsync();
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Only host can start the game.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public async Task HostOnlineAsync()
    {
        if (!CanStartNetworkAction())
            return;

        SetBusy(true);
        SetStatus("Initializing services...");

        try
        {
            await EnsureServicesReadyAsync();

            SetStatus("Creating Relay allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxClientConnections);

            SetStatus("Getting join code...");
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

            SetStatus("Starting host...");
            bool ok = NetworkManager.Singleton.StartHost();
            Debug.Log("[StartMenuUI] StartHost() returned: " + ok);

            if (!ok)
                throw new Exception("StartHost() returned false.");

            currentJoinCode = joinCode;
            SetJoinCode(joinCode);
            SetStatus("Host started. Loading game...");

            if (NetworkManager.Singleton.SceneManager == null)
            {
                Debug.LogError("[StartMenuUI] NGO SceneManager is null.");
                return;
            }

            // NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
            SetStatus($"Host ready. Share code: {joinCode}");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError("[StartMenuUI] Relay host error: " + ex);
            SetStatus("Relay host failed: " + ex.Message);
            ClearJoinCode();
        }
        catch (ServicesInitializationException ex)
        {
            Debug.LogError("[StartMenuUI] Services init error: " + ex);
            SetStatus("Services init failed: " + ex.Message);
            ClearJoinCode();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("[StartMenuUI] Auth error: " + ex);
            SetStatus("Sign-in failed: " + ex.Message);
            ClearJoinCode();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError("[StartMenuUI] Request failed: " + ex);
            SetStatus("Request failed: " + ex.Message);
            ClearJoinCode();
        }
        catch (Exception ex)
        {
            Debug.LogError("[StartMenuUI] Unexpected host error: " + ex);
            SetStatus("Host failed: " + ex.Message);
            ClearJoinCode();
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async Task JoinOnlineAsync()
    {
        if (!CanStartNetworkAction())
            return;

        if (joinCodeInput == null)
        {
            SetStatus("Join code input is missing.");
            return;
        }

        string joinCode = joinCodeInput.text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("Enter a join code.");
            return;
        }

        SetBusy(true);
        SetStatus("Initializing services...");

        try
        {
            await EnsureServicesReadyAsync();

            SetStatus("Joining Relay allocation...");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            Transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, connectionType));

            SetStatus("Starting client...");
            bool ok = NetworkManager.Singleton.StartClient();
            Debug.Log("[StartMenuUI] StartClient() returned: " + ok);

            if (!ok)
                throw new Exception("StartClient() returned false.");

            currentJoinCode = joinCode;
            SetJoinCode(joinCode);
            SetStatus("Joining game...");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError("[StartMenuUI] Relay join error: " + ex);
            SetStatus("Join failed: " + ex.Message);
            ClearJoinCode();
        }
        catch (ServicesInitializationException ex)
        {
            Debug.LogError("[StartMenuUI] Services init error: " + ex);
            SetStatus("Services init failed: " + ex.Message);
            ClearJoinCode();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("[StartMenuUI] Auth error: " + ex);
            SetStatus("Sign-in failed: " + ex.Message);
            ClearJoinCode();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError("[StartMenuUI] Request failed: " + ex);
            SetStatus("Request failed: " + ex.Message);
            ClearJoinCode();
        }
        catch (Exception ex)
        {
            Debug.LogError("[StartMenuUI] Unexpected join error: " + ex);
            SetStatus("Join failed: " + ex.Message);
            ClearJoinCode();
        }
        finally
        {
            SetBusy(false);
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[StartMenuUI] No NetworkManager.Singleton found.");
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            SetStatus("Not connected.");
            RefreshUI();
            return;
        }

        bool wasHost = NetworkManager.Singleton.IsHost;
        bool wasClientOnly = NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost;

        NetworkManager.Singleton.Shutdown();

        currentJoinCode = string.Empty;
        ClearJoinCode();

        if (wasHost)
            SetStatus("Host stopped.");
        else if (wasClientOnly)
            SetStatus("Disconnected from host.");
        else
            SetStatus("Disconnected.");

        RefreshUI();
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

    private bool CanStartNetworkAction()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[StartMenuUI] No NetworkManager.Singleton found.");
            return false;
        }

        if (Transport == null)
        {
            Debug.LogError("[StartMenuUI] No UnityTransport found on NetworkManager.");
            return false;
        }

        if (isBusy)
        {
            SetStatus("Please wait...");
            return false;
        }

        if (NetworkManager.Singleton.ShutdownInProgress)
        {
            Debug.LogWarning("[StartMenuUI] NetworkManager is still shutting down.");
            SetStatus("Still shutting down...");
            return false;
        }

        if (NetworkManager.Singleton.IsListening ||
            NetworkManager.Singleton.IsClient ||
            NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[StartMenuUI] NetworkManager already running.");
            SetStatus("Network session already running.");
            return false;
        }

        return true;
    }

    private void HandleServerStarted()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            SetStatus($"Host running. Code: {currentJoinCode}");
        }

        RefreshUI();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (NetworkManager.Singleton.IsHost)
                SetStatus($"Host connected. Code: {currentJoinCode}");
            else
                SetStatus("Connected to host.");
        }
        else if (NetworkManager.Singleton.IsHost)
        {
            SetStatus($"Client connected. Code: {currentJoinCode}");
        }

        RefreshUI();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetStatus("Disconnected.");
            ClearJoinCode();
        }
        else if (NetworkManager.Singleton.IsHost)
        {
            SetStatus($"A client disconnected. Code: {currentJoinCode}");
        }

        RefreshUI();
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;
        RefreshUI();
    }

    private void RefreshUI()
    {
        bool isListening = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (hostButton != null) hostButton.interactable = !isBusy && !isListening;
        if (joinButton != null) joinButton.interactable = !isBusy && !isListening;
        if (disconnectButton != null) disconnectButton.interactable = !isBusy && isListening;
        if (joinCodeInput != null) joinCodeInput.interactable = !isBusy && !isListening;
    }

    private void SetStatus(string message)
    {
        Debug.Log("[StartMenuUI] " + message);

        if (statusText != null)
            statusText.text = message;
    }

    private void SetJoinCode(string joinCode)
    {
        if (joinCodeText != null)
            joinCodeText.text = $"Join Code: {joinCode}";
    }

    private void ClearJoinCode()
    {
        if (joinCodeText != null)
            joinCodeText.text = "Join Code: -";
    }
}