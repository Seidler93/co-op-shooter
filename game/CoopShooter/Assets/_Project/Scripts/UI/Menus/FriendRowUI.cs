using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button inviteButton;

    private FriendProfile friend;
    private Action<FriendProfile> inviteClicked;

    private void OnEnable()
    {
        if (inviteButton != null)
            inviteButton.onClick.AddListener(HandleInviteClicked);
    }

    private void OnDisable()
    {
        if (inviteButton != null)
            inviteButton.onClick.RemoveListener(HandleInviteClicked);
    }

    public void Bind(FriendProfile friendProfile, Action<FriendProfile> onInviteClicked)
    {
        friend = friendProfile;
        inviteClicked = onInviteClicked;

        if (nameText != null)
        {
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.text = friendProfile != null ? friendProfile.BestName : "Unknown";
        }

        if (levelText != null)
        {
            levelText.textWrappingMode = TextWrappingModes.NoWrap;
            levelText.overflowMode = TextOverflowModes.Overflow;
            levelText.text = friendProfile != null ? $"LVL {Mathf.Max(1, friendProfile.Level)}" : "LVL -";
        }

        if (statusText != null)
        {
            statusText.textWrappingMode = TextWrappingModes.NoWrap;
            statusText.overflowMode = TextOverflowModes.Overflow;
            statusText.text = friendProfile != null && friendProfile.IsOnline ? "Online" : "Offline";
        }

        if (inviteButton != null)
            inviteButton.interactable = friendProfile != null;
    }

    private void HandleInviteClicked()
    {
        if (friend != null)
            inviteClicked?.Invoke(friend);
    }
}
