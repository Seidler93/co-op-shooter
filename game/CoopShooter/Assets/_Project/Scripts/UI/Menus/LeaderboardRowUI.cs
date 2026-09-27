using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text statusText;

    public void Bind(int rank, string playerName, int score, int kills, bool isLocalPlayer)
    {
        if (rankText != null)
            rankText.text = $"#{rank}";

        if (playerNameText != null)
            playerNameText.text = playerName;

        if (scoreText != null)
            scoreText.text = score.ToString();

        if (killsText != null)
            killsText.text = kills.ToString();

        if (statusText != null)
        {
            statusText.gameObject.SetActive(isLocalPlayer);
            statusText.text = isLocalPlayer ? "YOU" : string.Empty;
        }
    }
}
