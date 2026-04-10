using TMPro;
using UnityEngine;

public class PartyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text partyListText;

    private void Start()
    {
        TryHook();
        RefreshUI();
    }

    private void OnEnable()
    {
        TryHook();
        RefreshUI();
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void TryHook()
    {
        if (PartyManager.Instance == null) return;

        PartyManager.Instance.Players.OnListChanged -= HandlePartyListChanged;
        PartyManager.Instance.Players.OnListChanged += HandlePartyListChanged;
    }

    private void Unhook()
    {
        if (PartyManager.Instance == null) return;

        PartyManager.Instance.Players.OnListChanged -= HandlePartyListChanged;
    }

    private void HandlePartyListChanged(Unity.Netcode.NetworkListEvent<Unity.Collections.FixedString64Bytes> changeEvent)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (partyListText == null) return;

        if (PartyManager.Instance == null)
        {
            partyListText.text = "No party.";
            return;
        }

        if (PartyManager.Instance.Players.Count == 0)
        {
            partyListText.text = "No players connected.";
            return;
        }

        string text = "";
        for (int i = 0; i < PartyManager.Instance.Players.Count; i++)
        {
            text += PartyManager.Instance.Players[i].ToString();

            if (i < PartyManager.Instance.Players.Count - 1)
                text += "\n";
        }

        partyListText.text = text;
    }
}