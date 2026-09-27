using System.Collections;
using TMPro;
using UnityEngine;

public class RoundUI : MonoBehaviour
{
    public static RoundUI Instance { get; private set; }

    [Header("Persistent HUD")]
    [SerializeField] private TMP_Text hudRoundText;

    [Header("Round Start Popup")]
    [SerializeField] private TMP_Text popupRoundText;
    [SerializeField] private CanvasGroup popupGroup;

    private Coroutine showRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetRound(int roundNumber)
    {
        if (hudRoundText != null)
            hudRoundText.text = $"Round {roundNumber}";

        if (popupRoundText != null)
            popupRoundText.text = $"Round {roundNumber}";
    }

    public void ShowRoundStart(int roundNumber)
    {
        SetRound(roundNumber);

        if (popupRoundText == null || popupGroup == null)
            return;

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowRoundRoutine());
    }

    private IEnumerator ShowRoundRoutine()
    {
        popupGroup.alpha = 1f;

        Vector3 originalScale = popupRoundText.transform.localScale;
        popupRoundText.transform.localScale = Vector3.one * 1.4f;

        float growDuration = 0.8f;
        float timer = 0f;

        while (timer < growDuration)
        {
            timer += Time.deltaTime;
            float t = timer / growDuration;

            popupRoundText.transform.localScale =
                Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, t);

            yield return null;
        }

        yield return new WaitForSeconds(0.7f);

        float fadeDuration = 0.4f;
        float fadeTimer = 0f;

        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            popupGroup.alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
            yield return null;
        }

        popupGroup.alpha = 0f;
        popupRoundText.transform.localScale = originalScale;
        showRoutine = null;
    }
}