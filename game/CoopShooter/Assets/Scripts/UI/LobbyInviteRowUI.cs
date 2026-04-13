using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyInviteRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Button acceptButton;

    private LobbyInvite invite;
    private Action<LobbyInvite> acceptClicked;

    private void OnEnable()
    {
        if (acceptButton != null)
            acceptButton.onClick.AddListener(HandleAcceptClicked);
    }

    private void OnDisable()
    {
        if (acceptButton != null)
            acceptButton.onClick.RemoveListener(HandleAcceptClicked);
    }

    public void Bind(LobbyInvite lobbyInvite, Action<LobbyInvite> onAcceptClicked)
    {
        invite = lobbyInvite;
        acceptClicked = onAcceptClicked;

        if (titleText != null)
            titleText.text = invite != null ? $"Invite from {invite.SenderName}" : "Invite";

        if (detailText != null)
            detailText.text = invite != null ? $"Join code {invite.RelayJoinCode}" : string.Empty;

        if (acceptButton != null)
            acceptButton.interactable = invite != null;
    }

    private void HandleAcceptClicked()
    {
        if (invite != null)
            acceptClicked?.Invoke(invite);
    }
}
