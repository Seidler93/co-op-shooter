using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FriendsService : MonoBehaviour
{
    private const string FriendsKey = "project_z_mock_friends";
    private const string PlayerIdArg = "--projectz-user-id=";
    private const string EmailArg = "--projectz-email=";
    private const string UsernameArg = "--projectz-username=";
    private const string DisplayNameArg = "--projectz-display-name=";
    private const string SupabaseUrlArg = "--projectz-supabase-url=";
    private const string SupabaseAnonKeyArg = "--projectz-supabase-anon-key=";
    private const string SupabaseAccessTokenArg = "--projectz-supabase-access-token=";

    public static FriendsService Instance { get; private set; }

    [Header("Local Player")]
    [SerializeField] private string fallbackUserId = "local-player";
    [SerializeField] private string fallbackUsername = "ajseidler0526";
    [SerializeField] private float invitePollIntervalSeconds = 4f;

    private readonly List<FriendProfile> friends = new();
    private readonly List<LobbyInvite> pendingInvites = new();
    private SupabaseSocialClient supabase;
    private float nextInvitePollTime;

    public IReadOnlyList<FriendProfile> Friends => friends;
    public IReadOnlyList<LobbyInvite> PendingInvites => pendingInvites;
    public string CurrentUserId { get; private set; }
    public string CurrentEmail { get; private set; }
    public string CurrentUsername { get; private set; }
    public string CurrentDisplayName { get; private set; }

    public event Action FriendsChanged;
    public event Action InvitesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentUserId = ReadCommandLineValue(PlayerIdArg, fallbackUserId);
        CurrentEmail = ReadCommandLineValue(EmailArg, string.Empty);
        CurrentUsername = ReadCommandLineValue(UsernameArg, fallbackUsername);
        CurrentDisplayName = ReadCommandLineValue(DisplayNameArg, CurrentUsername);
        supabase = new SupabaseSocialClient(
            ReadCommandLineValue(SupabaseUrlArg, string.Empty),
            ReadCommandLineValue(SupabaseAnonKeyArg, string.Empty),
            ReadCommandLineValue(SupabaseAccessTokenArg, string.Empty));

        _ = BootstrapAsync();
    }

    private void Update()
    {
        if (!supabase.IsConfigured || Time.unscaledTime < nextInvitePollTime)
            return;

        nextInvitePollTime = Time.unscaledTime + invitePollIntervalSeconds;
        _ = RefreshInvitesAsync();
    }

    public async Task<FriendProfile> AddFriendByUsernameAsync(string username)
    {
        username = NormalizeUsername(username);
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Enter a username first.");

        FriendProfile existing = friends.Find(friend => string.Equals(friend.Username, username, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        if (!supabase.IsConfigured)
        {
            var mockFriend = new FriendProfile
            {
                UserId = $"mock-{username.ToLowerInvariant()}",
                Username = username,
                DisplayName = username,
                Level = 1,
                IsOnline = true
            };

            friends.Add(mockFriend);
            SaveFriends();
            FriendsChanged?.Invoke();
            return mockFriend;
        }

        FriendProfile friend = await supabase.FindProfileAsync(username, CurrentUserId);
        await supabase.AddFriendAsync(CurrentUserId, friend);

        await RefreshFriendsAsync();

        return friend;
    }

    public async Task SendLobbyInviteAsync(FriendProfile friend, string relayJoinCode)
    {
        if (friend == null)
            throw new ArgumentNullException(nameof(friend));

        if (string.IsNullOrWhiteSpace(relayJoinCode))
            throw new InvalidOperationException("Start or host a party before sending an invite.");

        if (!supabase.IsConfigured)
        {
            Debug.Log($"[FriendsService] Mock invite sent to {friend.Username}. Join code: {relayJoinCode}");
            return;
        }

        await supabase.SendLobbyInviteAsync(CurrentUserId, friend, relayJoinCode);
        Debug.Log($"[FriendsService] Invite sent to {friend.Username}. Join code: {relayJoinCode}");
    }

    public void AddMockIncomingInvite(string fromUsername, string relayJoinCode)
    {
        fromUsername = NormalizeUsername(fromUsername);
        if (string.IsNullOrWhiteSpace(fromUsername) || string.IsNullOrWhiteSpace(relayJoinCode))
            return;

        pendingInvites.Add(new LobbyInvite
        {
            InviteId = Guid.NewGuid().ToString("N"),
            FromUserId = $"mock-{fromUsername.ToLowerInvariant()}",
            FromUsername = fromUsername,
            RelayJoinCode = relayJoinCode.Trim().ToUpperInvariant(),
            CreatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        InvitesChanged?.Invoke();
    }

    public async void MarkInviteAccepted(LobbyInvite invite)
    {
        if (invite == null)
            return;

        if (supabase.IsConfigured)
            await supabase.MarkInviteAcceptedAsync(invite.InviteId);

        pendingInvites.RemoveAll(item => item.InviteId == invite.InviteId);
        InvitesChanged?.Invoke();
    }

    private async Task BootstrapAsync()
    {
        if (!supabase.IsConfigured)
        {
            LoadFriends();
            FriendsChanged?.Invoke();
            return;
        }

        try
        {
            await supabase.UpsertProfileAsync(CurrentUserId, CurrentEmail, CurrentDisplayName);
            await RefreshFriendsAsync();
            await RefreshInvitesAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FriendsService] Supabase social bootstrap failed, falling back to local data: {ex.Message}");
            LoadFriends();
            FriendsChanged?.Invoke();
        }
    }

    private async Task RefreshFriendsAsync()
    {
        if (!supabase.IsConfigured)
            return;

        friends.Clear();
        friends.AddRange(await supabase.FetchFriendsAsync(CurrentUserId));
        FriendsChanged?.Invoke();
    }

    private async Task RefreshInvitesAsync()
    {
        if (!supabase.IsConfigured)
            return;

        try
        {
            pendingInvites.Clear();
            pendingInvites.AddRange(await supabase.FetchPendingInvitesAsync(CurrentUserId));
            InvitesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FriendsService] Could not refresh invites: {ex.Message}");
        }
    }

    private void LoadFriends()
    {
        friends.Clear();

        string stored = PlayerPrefs.GetString(FriendsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(stored))
            return;

        string[] usernames = stored.Split('|', StringSplitOptions.RemoveEmptyEntries);
        foreach (string username in usernames)
        {
            string normalized = NormalizeUsername(username);
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            friends.Add(new FriendProfile
            {
                UserId = $"mock-{normalized.ToLowerInvariant()}",
                Username = normalized,
                DisplayName = normalized,
                Level = 1,
                IsOnline = true
            });
        }
    }

    private void SaveFriends()
    {
        List<string> usernames = new();
        foreach (FriendProfile friend in friends)
        {
            if (!string.IsNullOrWhiteSpace(friend.Username))
                usernames.Add(friend.Username);
        }

        PlayerPrefs.SetString(FriendsKey, string.Join("|", usernames));
        PlayerPrefs.Save();
    }

    private static string NormalizeUsername(string username)
    {
        return string.IsNullOrWhiteSpace(username) ? string.Empty : username.Trim();
    }

    private static string ReadCommandLineValue(string prefix, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg.Substring(prefix.Length).Trim();
        }

        return fallback;
    }
}
