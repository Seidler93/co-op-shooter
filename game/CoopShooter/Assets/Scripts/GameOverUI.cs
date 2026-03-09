using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private void Awake()
    {
        Hide();

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        bool isHostOrServer =
            NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer);

        if (restartButton != null)
            restartButton.gameObject.SetActive(isHostOrServer);

        BlockLocalGameplay();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void BlockLocalGameplay()
    {
        var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var pc in playerControllers)
        {
            var netObj = pc.GetComponent<NetworkObject>();
            if (netObj == null) continue;
            if (!netObj.IsOwner) continue;

            pc.SetGameplayInputBlocked(true);
        }
    }

    private void OnRestartClicked()
    {
        if (RoundManager.Instance == null) return;
        RoundManager.Instance.RequestRestartRpc();
    }

    private void OnMenuClicked()
    {
        if (RoundManager.Instance == null) return;
        RoundManager.Instance.RequestReturnToMenuRpc();
    }
}