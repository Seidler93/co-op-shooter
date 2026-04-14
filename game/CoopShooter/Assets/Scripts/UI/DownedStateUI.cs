using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownedStateUI : MonoBehaviour
{
    public static DownedStateUI Instance { get; private set; }

    [Header("Downed Panel")]
    [SerializeField] private GameObject downedPanelRoot;
    [SerializeField] private TMP_Text downedTimerText;
    [SerializeField] private TMP_Text downedStatusText;

    [Header("Revive Prompt")]
    [SerializeField] private GameObject revivePromptRoot;
    [SerializeField] private TMP_Text revivePromptText;
    [SerializeField] private Image reviveProgressFill;

    private PlayerHealth localPlayerHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HideRevivePrompt();
        SetDownedPanelVisible(false);
    }

    private void Update()
    {
        if (localPlayerHealth == null)
        {
            SetDownedPanelVisible(false);
            return;
        }

        bool isDowned = localPlayerHealth.IsDowned;
        SetDownedPanelVisible(isDowned);

        if (!isDowned)
            return;

        if (downedStatusText != null)
            downedStatusText.text = "Downed - wait for a revive";

        if (downedTimerText != null)
            downedTimerText.text = $"Bleedout in {localPlayerHealth.GetDownedSecondsRemaining():0.0}s";
    }

    public void BindLocalPlayer(PlayerHealth playerHealth)
    {
        localPlayerHealth = playerHealth;
    }

    public void ShowRevivePrompt(string prompt, float progress01)
    {
        if (revivePromptRoot != null)
            revivePromptRoot.SetActive(true);

        if (revivePromptText != null)
            revivePromptText.text = prompt;

        if (reviveProgressFill != null)
            reviveProgressFill.fillAmount = Mathf.Clamp01(progress01);
    }

    public void HideRevivePrompt()
    {
        if (revivePromptRoot != null)
            revivePromptRoot.SetActive(false);

        if (reviveProgressFill != null)
            reviveProgressFill.fillAmount = 0f;
    }

    private void SetDownedPanelVisible(bool visible)
    {
        if (downedPanelRoot != null)
            downedPanelRoot.SetActive(visible);
    }
}
