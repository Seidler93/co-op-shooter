using TMPro;
using UnityEngine;

public class PartyMemberCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private TMP_Text playerStatusText;

    public void Bind(string playerName, int level, string status)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;

        if (playerLevelText != null)
            playerLevelText.text = $"LVL {level}";

        if (playerStatusText != null)
        {
            bool hasStatus = !string.IsNullOrWhiteSpace(status);
            playerStatusText.gameObject.SetActive(hasStatus);
            playerStatusText.text = status;
        }
    }
}
