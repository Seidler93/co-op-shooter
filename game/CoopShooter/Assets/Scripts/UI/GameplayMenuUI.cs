using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameplayMenuUI : MonoBehaviour
{
    public bool IsOpen => isOpen;

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text headerText;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button returnToMenuButton;

    [Header("Debug Input")]
    [SerializeField] private bool enableZeroKeyInEditor = true;
    [SerializeField] private bool enableZeroKeyInDevelopmentBuild = true;

    private PlayerController localPlayerController;
    private bool isOpen;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(CloseMenu);

        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    private void Update()
    {
        if (!ShouldAllowMenuToggle())
            return;

        if (WasTogglePressed())
            ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (isOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (isOpen)
            return;

        isOpen = true;

        if (root != null)
            root.SetActive(true);

        if (headerText != null)
            headerText.text = "Mission Menu";

        FindLocalPlayerController();
        localPlayerController?.SetGameplayInputBlocked(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (root != null)
            root.SetActive(false);

        FindLocalPlayerController();
        localPlayerController?.SetGameplayInputBlocked(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool ShouldAllowMenuToggle()
    {
        if (GameOverUIIsVisible())
            return false;

        return Keyboard.current != null;
    }

    private bool WasTogglePressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        if (keyboard.escapeKey.wasPressedThisFrame)
            return true;

        bool allowZeroKey = (Application.isEditor && enableZeroKeyInEditor)
            || (Debug.isDebugBuild && enableZeroKeyInDevelopmentBuild);

        return allowZeroKey && keyboard.digit0Key.wasPressedThisFrame;
    }

    private void FindLocalPlayerController()
    {
        if (localPlayerController != null)
            return;

        PlayerController[] playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController playerController in playerControllers)
        {
            var networkObject = playerController.GetComponent<Unity.Netcode.NetworkObject>();
            if (networkObject != null && networkObject.IsOwner)
            {
                localPlayerController = playerController;
                return;
            }
        }
    }

    private void ReturnToMenu()
    {
        if (RoundManager.Instance == null)
            return;

        CloseMenu();
        RoundManager.Instance.RequestReturnToMenuRpc();
    }

    private bool GameOverUIIsVisible()
    {
        GameOverUI gameOverUI = FindFirstObjectByType<GameOverUI>();
        return gameOverUI != null && gameOverUI.IsVisible;
    }
}
