using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class RelayMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Button hostOnlineButton;
    [SerializeField] private Button joinOnlineButton;
    [SerializeField] private Button disconnectButton;

    [Header("Relay")]
    [Tooltip("For a 2-player game (host + 1 client), set this to 1.")]
    [SerializeField] private int maxClientConnections = 1;

    [Tooltip("Use dtls for normal desktop builds. Use wss for Web builds.")]
    [SerializeField] private string connectionType = "dtls";

    [Header("References")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport unityTransport;

    private bool isBusy;
    private bool callbacksRegistered;
    private string currentJoinCode = string.Empty;

    private void Awake()
    {
        AutoAssignReferences();
        ValidateReferences();
        ClearJoinCodeUI();
        SetStatus("Ready.");
    }

    private void OnEnable()
    {
        RegisterButtonListeners();
        RegisterNetworkCallbacks();
        RefreshButtonState();
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
        UnregisterNetworkCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();
        UnregisterNetworkCallbacks();
    }

    private void AutoAssignReferences()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        if (unityTransport == null && networkManager != null)
        {
            unityTransport = networkManager.GetComponent<UnityTransport>();
        }
    }

    private void ValidateReferences()
    {
        if (networkManager == null)
        {
            Debug.LogError("[RelayMenuController] NetworkManager reference is missing.");
        }

        if (unityTransport == null)
        {
            Debug.LogError("[RelayMenuController] UnityTransport reference is missing.");
        }

        if (joinCodeInput == null) Debug.LogError("[RelayMenuController] Join Code Input is missing.");
        if (statusText == null) Debug.LogError("[RelayMenuController] Status Text is missing.");
        if (joinCodeText == null) Debug.LogError("[RelayMenuController] Join Code Text is missing.");
        if (hostOnlineButton == null) Debug.LogError("[RelayMenuController] Host Online Button is missing.");
        if (joinOnlineButton == null) Debug.LogError("[RelayMenuController] Join Online Button is missing.");
        if (disconnectButton == null) Debug.LogError("[RelayMenuController] Disconnect Button is missing.");
    }

    private void RegisterButtonListeners()
    {
        if (hostOnlineButton != null)
            hostOnlineButton.onClick.AddListener(OnHostOnlineClicked);

        if (joinOnlineButton != null)
            joinOnlineButton.onClick.AddListener(OnJoinOnlineClicked);

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectClicked);
    }

    private void UnregisterButtonListeners()
    {
        if (hostOnlineButton != null)
            hostOnlineButton.onClick.RemoveListener(OnHostOnlineClicked);

        if (joinOnlineButton != null)
            joinOnlineButton.onClick.RemoveListener(OnJoinOnlineClicked);

        if (disconnectButton != null)
            disconnectButton.onClick.RemoveListener(OnDisconnectClicked);
    }

    private void RegisterNetworkCallbacks()
    {
        if (callbacksRegistered || networkManager == null)
            return;

        networkManager.OnServerStarted += HandleServerStarted;
        networkManager.OnClientConnectedCallback += HandleClientConnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        callbacksRegistered = true;
    }

    private void UnregisterNetworkCallbacks()
    {
        if (!callbacksRegistered || networkManager == null)
            return;

        networkManager.OnServerStarted -= HandleServerStarted;
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;

        callbacksRegistered = false;
    }

    private async void OnHostOnlineClicked()
    {
        await HostOnlineAsync();
    }

    private async void OnJoinOnlineClicked()
    {
        await JoinOnlineAsync();
    }

    private void OnDisconnectClicked()
    {
        Disconnect();
    }

    public async Task HostOnlineAsync()
    {
        if (!CanStartNetworkAction())
            return;

        SetBusy(true);
        SetStatus("Initializing services...");

        try
        {
            await EnsureUnityServicesSignedInAsync();

            SetStatus("Creating Relay allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxClientConnections);

            SetStatus("Getting join code...");
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            unityTransport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, connectionType)
            );

            SetStatus("Starting host...");
            bool started = networkManager.StartHost();

            if (!started)
            {
                throw new Exception("NetworkManager.StartHost() returned false.");
            }

            currentJoinCode = joinCode;
            SetJoinCode(joinCode);
            SetStatus($"Host started. Share code: {joinCode}");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"[RelayMenuController] Relay error while hosting: {ex}");
            SetStatus($"Relay error: {ex.Message}");
            ClearJoinCodeUI();
        }
        catch (ServicesInitializationException ex)
        {
            Debug.LogError($"[RelayMenuController] Services initialization error: {ex}");
            SetStatus($"Services init failed: {ex.Message}");
            ClearJoinCodeUI();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[RelayMenuController] Authentication error: {ex}");
            SetStatus($"Auth failed: {ex.Message}");
            ClearJoinCodeUI();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"[RelayMenuController] Request failed: {ex}");
            SetStatus($"Request failed: {ex.Message}");
            ClearJoinCodeUI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RelayMenuController] Unexpected host error: {ex}");
            SetStatus($"Host failed: {ex.Message}");
            ClearJoinCodeUI();
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

        string joinCode = joinCodeInput != null
            ? joinCodeInput.text.Trim().ToUpperInvariant()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("Enter a valid join code first.");
            return;
        }

        SetBusy(true);
        SetStatus("Initializing services...");

        try
        {
            await EnsureUnityServicesSignedInAsync();

            SetStatus("Joining Relay allocation...");
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            unityTransport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, connectionType)
            );

            SetStatus("Starting client...");
            bool started = networkManager.StartClient();

            if (!started)
            {
                throw new Exception("NetworkManager.StartClient() returned false.");
            }

            currentJoinCode = joinCode;
            SetJoinCode(joinCode);
            SetStatus($"Joining with code: {joinCode}");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"[RelayMenuController] Relay error while joining: {ex}");
            SetStatus($"Join failed: {ex.Message}");
            ClearJoinCodeUI();
        }
        catch (ServicesInitializationException ex)
        {
            Debug.LogError($"[RelayMenuController] Services initialization error: {ex}");
            SetStatus($"Services init failed: {ex.Message}");
            ClearJoinCodeUI();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"[RelayMenuController] Authentication error: {ex}");
            SetStatus($"Auth failed: {ex.Message}");
            ClearJoinCodeUI();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"[RelayMenuController] Request failed: {ex}");
            SetStatus($"Request failed: {ex.Message}");
            ClearJoinCodeUI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RelayMenuController] Unexpected join error: {ex}");
            SetStatus($"Join failed: {ex.Message}");
            ClearJoinCodeUI();
        }
        finally
        {
            SetBusy(false);
        }
    }

    public void Disconnect()
    {
        if (networkManager == null)
        {
            SetStatus("No NetworkManager found.");
            return;
        }

        if (!networkManager.IsListening)
        {
            SetStatus("Not connected.");
            ClearJoinCodeUI();
            RefreshButtonState();
            return;
        }

        bool wasHost = networkManager.IsHost;
        bool wasClientOnly = networkManager.IsClient && !networkManager.IsHost;

        networkManager.Shutdown();
        currentJoinCode = string.Empty;
        ClearJoinCodeUI();

        if (wasHost)
        {
            SetStatus("Host stopped.");
        }
        else if (wasClientOnly)
        {
            SetStatus("Disconnected from host.");
        }
        else
        {
            SetStatus("Disconnected.");
        }

        RefreshButtonState();
    }

    private async Task EnsureUnityServicesSignedInAsync()
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
        if (isBusy)
        {
            SetStatus("Please wait...");
            return false;
        }

        if (networkManager == null || unityTransport == null)
        {
            SetStatus("Missing NetworkManager or UnityTransport reference.");
            return false;
        }

        if (networkManager.IsListening)
        {
            SetStatus("A network session is already running.");
            return false;
        }

        return true;
    }

    private void HandleServerStarted()
    {
        if (networkManager != null && networkManager.IsHost)
        {
            SetStatus($"Host running. Join code: {currentJoinCode}");
        }

        RefreshButtonState();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (networkManager == null)
            return;

        if (clientId == networkManager.LocalClientId)
        {
            if (networkManager.IsHost)
            {
                SetStatus($"Host connected locally. Join code: {currentJoinCode}");
            }
            else
            {
                SetStatus("Connected to host successfully.");
            }
        }
        else if (networkManager.IsHost)
        {
            SetStatus($"Client connected. Join code: {currentJoinCode}");
        }

        RefreshButtonState();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (networkManager == null)
            return;

        if (clientId == networkManager.LocalClientId)
        {
            ClearJoinCodeUI();
            SetStatus("Disconnected from session.");
        }
        else if (networkManager.IsHost)
        {
            SetStatus($"A client disconnected. Join code: {currentJoinCode}");
        }

        RefreshButtonState();
    }

    private void SetBusy(bool value)
    {
        isBusy = value;
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        bool isListening = networkManager != null && networkManager.IsListening;

        if (hostOnlineButton != null)
            hostOnlineButton.interactable = !isBusy && !isListening;

        if (joinOnlineButton != null)
            joinOnlineButton.interactable = !isBusy && !isListening;

        if (disconnectButton != null)
            disconnectButton.interactable = !isBusy && isListening;

        if (joinCodeInput != null)
            joinCodeInput.interactable = !isBusy && !isListening;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log($"[RelayMenuController] {message}");
    }

    private void SetJoinCode(string joinCode)
    {
        if (joinCodeText != null)
        {
            joinCodeText.text = $"Join Code: {joinCode}";
        }
    }

    private void ClearJoinCodeUI()
    {
        currentJoinCode = string.Empty;

        if (joinCodeText != null)
        {
            joinCodeText.text = "Join Code: -";
        }
    }
}