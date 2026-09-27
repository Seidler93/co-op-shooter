using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendsPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button friendsTabButton;
    [SerializeField] private Button notificationsTabButton;
    [SerializeField] private Button addFriendTabButton;
    [SerializeField] private GameObject friendsTabRoot;
    [SerializeField] private GameObject notificationsTabRoot;
    [SerializeField] private GameObject addFriendTabRoot;

    [Header("Add Friend")]
    [SerializeField] private TMP_InputField addFriendInput;
    [SerializeField] private Button addFriendButton;

    [Header("Friends List")]
    [SerializeField] private Transform friendsListRoot;
    [SerializeField] private FriendRowUI friendRowPrefab;
    [SerializeField] private TMP_Text emptyFriendsText;

    [Header("Notifications")]
    [SerializeField] private Transform notificationsListRoot;
    [SerializeField] private LobbyInviteRowUI inviteRowPrefab;
    [SerializeField] private TMP_Text emptyNotificationsText;

    [Header("References")]
    [SerializeField] private FriendsService friendsService;
    [SerializeField] private MainMenuController playController;

    private bool isInvitePickerMode;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        EnsureService();
        ValidateTabHierarchy();
        ClearStatus();
        Hide();
    }

    private void OnEnable()
    {
        WireButtons();
        SubscribeService();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnwireButtons();
        UnsubscribeService();
    }

    public void OpenFriends()
    {
        isInvitePickerMode = false;
        ShowPanel("Friends");
        ShowFriendsTab();
    }

    public void OpenInvitePicker()
    {
        isInvitePickerMode = true;
        ShowPanel("Invite Friends");
        ShowFriendsTab();
        SetStatus("Select a friend to invite to your lobby.");
    }

    public void OpenNotifications()
    {
        isInvitePickerMode = false;
        ShowPanel("Notifications");
        ShowNotificationsTab();
    }

    public void OpenAddFriend()
    {
        isInvitePickerMode = false;
        ShowPanel("Add Friend");
        ShowAddFriendTab();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Close()
    {
        Hide();
        playController?.ShowMainPanel();
    }

    public void AddMockIncomingInviteFromCurrentParty()
    {
        EnsureService();

        if (friendsService == null || playController == null)
            return;

        friendsService.AddMockIncomingInvite("Mock Friend", playController.CurrentJoinCode);
        OpenNotifications();
    }

    private void ShowPanel(string title)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        RefreshAll();
    }

    private void ShowFriendsTab()
    {
        SetActiveTab(friendsTabRoot);
        RefreshFriends();
    }

    private void ShowNotificationsTab()
    {
        SetActiveTab(notificationsTabRoot);
        RefreshInvites();
    }

    private void ShowAddFriendTab()
    {
        SetActiveTab(addFriendTabRoot);
    }

    private void SetActiveTab(GameObject activeRoot)
    {
        if (friendsTabRoot != null)
            friendsTabRoot.SetActive(friendsTabRoot == activeRoot);

        if (notificationsTabRoot != null)
            notificationsTabRoot.SetActive(notificationsTabRoot == activeRoot);

        if (addFriendTabRoot != null)
            addFriendTabRoot.SetActive(addFriendTabRoot == activeRoot);
    }

    private async void HandleAddFriendClicked()
    {
        await AddFriendAsync();
    }

    private async Task AddFriendAsync()
    {
        EnsureService();

        if (friendsService == null)
        {
            SetStatus("Friends service missing.");
            return;
        }

        string username = addFriendInput != null ? addFriendInput.text : string.Empty;

        try
        {
            FriendProfile friend = await friendsService.AddFriendByUsernameAsync(username);

            if (addFriendInput != null)
                addFriendInput.text = string.Empty;

            SetStatus($"Added {friend.BestName}.");
            RefreshFriends();
            ShowFriendsTab();
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private async void HandleInviteClicked(FriendProfile friend)
    {
        EnsureService();

        if (friendsService == null || playController == null)
        {
            SetStatus("Invite system is missing a service or play controller.");
            return;
        }

        string joinCode = playController.CurrentJoinCode;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("Creating lobby...");
            joinCode = await playController.GetOrCreateInviteJoinCodeAsync();
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus("Could not create a lobby for the invite.");
            return;
        }

        try
        {
            await friendsService.SendLobbyInviteAsync(friend, joinCode);
            SetStatus($"Invite sent to {friend.BestName}.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private async void HandleAcceptInviteClicked(LobbyInvite invite)
    {
        if (invite == null)
            return;

        if (playController == null)
        {
            SetStatus("Play controller missing.");
            return;
        }

        SetStatus($"Joining {invite.SenderName}...");
        await playController.JoinInvitedPartyAsync(invite.RelayJoinCode);

        friendsService?.MarkInviteAccepted(invite);
        RefreshInvites();
    }

    private void RefreshAll()
    {
        RefreshFriends();
        RefreshInvites();
    }

    private void RefreshFriends()
    {
        EnsureService();
        ClearChildren(friendsListRoot);

        int count = friendsService != null ? friendsService.Friends.Count : 0;
        if (emptyFriendsText != null)
        {
            emptyFriendsText.gameObject.SetActive(count == 0);
            emptyFriendsText.text = isInvitePickerMode
                ? "Add a friend before sending invites."
                : "No friends added yet.";
        }

        if (friendsService == null || friendsListRoot == null || friendRowPrefab == null)
            return;

        foreach (FriendProfile friend in friendsService.Friends)
        {
            FriendRowUI row = Instantiate(friendRowPrefab, friendsListRoot);
            row.Bind(friend, HandleInviteClicked);
        }
    }

    private void RefreshInvites()
    {
        EnsureService();
        ClearChildren(notificationsListRoot);

        int count = friendsService != null ? friendsService.PendingInvites.Count : 0;
        if (emptyNotificationsText != null)
            emptyNotificationsText.gameObject.SetActive(count == 0);

        if (friendsService == null || notificationsListRoot == null || inviteRowPrefab == null)
            return;

        foreach (LobbyInvite invite in friendsService.PendingInvites)
        {
            LobbyInviteRowUI row = Instantiate(inviteRowPrefab, notificationsListRoot);
            row.Bind(invite, HandleAcceptInviteClicked);
        }
    }

    private void EnsureService()
    {
        if (friendsService != null)
            return;

        friendsService = FriendsService.Instance;

        if (friendsService == null)
        {
            var serviceRoot = new GameObject("FriendsService");
            friendsService = serviceRoot.AddComponent<FriendsService>();
        }
    }

    private void SubscribeService()
    {
        EnsureService();

        if (friendsService == null)
            return;

        friendsService.FriendsChanged -= RefreshFriends;
        friendsService.FriendsChanged += RefreshFriends;
        friendsService.InvitesChanged -= RefreshInvites;
        friendsService.InvitesChanged += RefreshInvites;
    }

    private void UnsubscribeService()
    {
        if (friendsService == null)
            return;

        friendsService.FriendsChanged -= RefreshFriends;
        friendsService.InvitesChanged -= RefreshInvites;
    }

    private void WireButtons()
    {
        if (friendsTabButton != null)
            friendsTabButton.onClick.AddListener(ShowFriendsTab);

        if (notificationsTabButton != null)
            notificationsTabButton.onClick.AddListener(ShowNotificationsTab);

        if (addFriendTabButton != null)
            addFriendTabButton.onClick.AddListener(ShowAddFriendTab);

        if (addFriendButton != null)
            addFriendButton.onClick.AddListener(HandleAddFriendClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void UnwireButtons()
    {
        if (friendsTabButton != null)
            friendsTabButton.onClick.RemoveListener(ShowFriendsTab);

        if (notificationsTabButton != null)
            notificationsTabButton.onClick.RemoveListener(ShowNotificationsTab);

        if (addFriendTabButton != null)
            addFriendTabButton.onClick.RemoveListener(ShowAddFriendTab);

        if (addFriendButton != null)
            addFriendButton.onClick.RemoveListener(HandleAddFriendClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private void ValidateTabHierarchy()
    {
        WarnIfButtonIsInsideTabRoot(friendsTabButton, "Friends");
        WarnIfButtonIsInsideTabRoot(notificationsTabButton, "Notifications");
        WarnIfButtonIsInsideTabRoot(addFriendTabButton, "Add Friend");
    }

    private void WarnIfButtonIsInsideTabRoot(Button button, string buttonName)
    {
        if (button == null)
            return;

        if ((friendsTabRoot != null && button.transform.IsChildOf(friendsTabRoot.transform)) ||
            (notificationsTabRoot != null && button.transform.IsChildOf(notificationsTabRoot.transform)) ||
            (addFriendTabRoot != null && button.transform.IsChildOf(addFriendTabRoot.transform)))
        {
            Debug.LogWarning($"[FriendsPanelUI] {buttonName} tab button is inside a tab content root. Move tab buttons outside FriendsTabRoot, NotificationsTabRoot, and AddFriendTabRoot so switching tabs does not hide them.");
        }
    }

    private void SetStatus(string message)
    {
        bool hasMessage = !string.IsNullOrWhiteSpace(message);

        if (statusText != null)
        {
            statusText.gameObject.SetActive(hasMessage);
            statusText.text = message;
        }

        if (hasMessage)
            Debug.Log($"[FriendsPanelUI] {message}");
    }

    private void ClearStatus()
    {
        SetStatus(string.Empty);
    }

    private static void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
