using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private NetworkPlayer localPlayer;

    private void Start()
    {
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        while (localPlayer == null)
        {
            localPlayer = FindLocalPlayer();
            yield return null;
        }

        localPlayer.Score.OnValueChanged += OnScoreChanged;
        UpdateScore(localPlayer.Score.Value);

        Debug.Log($"[ScoreUI] Bound to local player: {localPlayer.name} | starting score = {localPlayer.Score.Value}");
    }

    private NetworkPlayer FindLocalPlayer()
    {
        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            if (player.IsOwner)
                return player;
        }

        return null;
    }

    private void OnDestroy()
    {
        if (localPlayer != null)
            localPlayer.Score.OnValueChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int oldScore, int newScore)
    {
        Debug.Log($"[ScoreUI] Score changed: {oldScore} -> {newScore}");
        UpdateScore(newScore);
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }
}