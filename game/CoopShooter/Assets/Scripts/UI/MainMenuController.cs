using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject onlinePanel;
    [SerializeField] private GameObject barracksPanel;
    [SerializeField] private GameObject challengesPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        ShowMainPanel();
    }

    public void ShowMainPanel()
    {
        HideAllPanels();
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void ShowOnlinePanel()
    {
        // HideAllPanels();
        if (onlinePanel != null) onlinePanel.SetActive(true);
    }

    public void ToggleOnlinePanel()
    {
        if (onlinePanel == null) return;

        bool isActive = onlinePanel.gameObject.activeSelf;
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

    private void HideAllPanels()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (onlinePanel != null) onlinePanel.SetActive(false);
        if (barracksPanel != null) barracksPanel.SetActive(false);
        if (challengesPanel != null) challengesPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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
}