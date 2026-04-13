using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public sealed class SupabaseSocialClient
{
    private readonly string supabaseUrl;
    private readonly string anonKey;
    private readonly string accessToken;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(supabaseUrl) &&
        !string.IsNullOrWhiteSpace(anonKey) &&
        !string.IsNullOrWhiteSpace(accessToken);

    public SupabaseSocialClient(string supabaseUrl, string anonKey, string accessToken)
    {
        this.supabaseUrl = supabaseUrl?.TrimEnd('/');
        this.anonKey = anonKey;
        this.accessToken = accessToken;
    }

    public async Task UpsertProfileAsync(string userId, string email, string displayName)
    {
        var payload = new[]
        {
            new ProfileRow
            {
                Id = userId,
                Email = email,
                DisplayName = displayName,
                UpdatedAt = DateTime.UtcNow.ToString("O")
            }
        };

        await SendAsync(
            "POST",
            "/rest/v1/profiles?on_conflict=id",
            payload,
            ("Prefer", "resolution=merge-duplicates"));
    }

    public async Task<FriendProfile> FindProfileAsync(string usernameOrEmail, string currentUserId)
    {
        string search = usernameOrEmail?.Trim();
        if (string.IsNullOrWhiteSpace(search))
            throw new ArgumentException("Enter a username first.");

        string encoded = UnityWebRequest.EscapeURL(search);
        string path = $"/rest/v1/profiles?select=id,display_name&display_name=ilike.{encoded}&limit=1";
        string json = await SendAsync("GET", path);
        List<ProfileRow> rows = JsonConvert.DeserializeObject<List<ProfileRow>>(json) ?? new List<ProfileRow>();

        if (rows.Count == 0)
            throw new InvalidOperationException("No player found with that username.");

        ProfileRow row = rows[0];
        if (string.Equals(row.Id, currentUserId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("You cannot add yourself as a friend.");

        return ToFriendProfile(row);
    }

    public async Task AddFriendAsync(string ownerId, FriendProfile friend)
    {
        var payload = new[]
        {
            new FriendLinkRow
            {
                OwnerId = ownerId,
                FriendId = friend.UserId
            }
        };

        await SendAsync(
            "POST",
            "/rest/v1/friend_links?on_conflict=owner_id,friend_id",
            payload,
            ("Prefer", "resolution=ignore-duplicates"));
    }

    public async Task<List<FriendProfile>> FetchFriendsAsync(string ownerId)
    {
        string path = $"/rest/v1/friend_links?select=friend_id&owner_id=eq.{UnityWebRequest.EscapeURL(ownerId)}&order=created_at.desc";
        string json = await SendAsync("GET", path);
        List<FriendLinkRow> rows = JsonConvert.DeserializeObject<List<FriendLinkRow>>(json) ?? new List<FriendLinkRow>();
        var friends = new List<FriendProfile>();
        var friendIds = new List<string>();

        foreach (FriendLinkRow row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.FriendId))
                friendIds.Add(row.FriendId);
        }

        Dictionary<string, ProfileRow> profilesById = await FetchProfilesByIdsAsync(friendIds);
        foreach (string friendId in friendIds)
        {
            if (profilesById.TryGetValue(friendId, out ProfileRow profile))
                friends.Add(ToFriendProfile(profile));
        }

        return friends;
    }

    public async Task SendLobbyInviteAsync(string senderId, FriendProfile friend, string relayJoinCode)
    {
        var payload = new[]
        {
            new LobbyInviteRow
            {
                SenderId = senderId,
                RecipientId = friend.UserId,
                RelayJoinCode = relayJoinCode.Trim().ToUpperInvariant(),
                Status = "pending"
            }
        };

        await SendAsync("POST", "/rest/v1/lobby_invites", payload);
    }

    public async Task<List<LobbyInvite>> FetchPendingInvitesAsync(string recipientId)
    {
        string path = $"/rest/v1/lobby_invites?select=id,sender_id,relay_join_code,created_at&recipient_id=eq.{UnityWebRequest.EscapeURL(recipientId)}&status=eq.pending&order=created_at.desc";
        string json = await SendAsync("GET", path);
        List<LobbyInviteWithSenderRow> rows = JsonConvert.DeserializeObject<List<LobbyInviteWithSenderRow>>(json) ?? new List<LobbyInviteWithSenderRow>();
        var invites = new List<LobbyInvite>();
        var senderIds = new List<string>();

        foreach (LobbyInviteWithSenderRow row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.SenderId))
                senderIds.Add(row.SenderId);
        }

        Dictionary<string, ProfileRow> profilesById = await FetchProfilesByIdsAsync(senderIds);

        foreach (LobbyInviteWithSenderRow row in rows)
        {
            invites.Add(new LobbyInvite
            {
                InviteId = row.Id,
                FromUserId = row.SenderId,
                FromUsername = profilesById.TryGetValue(row.SenderId ?? string.Empty, out ProfileRow senderProfile)
                    ? senderProfile.DisplayName ?? "Unknown player"
                    : "Unknown player",
                RelayJoinCode = row.RelayJoinCode,
                CreatedAtUnixSeconds = ParseUnixSeconds(row.CreatedAt)
            });
        }

        return invites;
    }

    public async Task MarkInviteAcceptedAsync(string inviteId)
    {
        if (string.IsNullOrWhiteSpace(inviteId))
            return;

        await SendAsync(
            "PATCH",
            $"/rest/v1/lobby_invites?id=eq.{UnityWebRequest.EscapeURL(inviteId)}",
            new { status = "accepted" });
    }

    private async Task<string> SendAsync(string method, string path, object body = null, params (string Name, string Value)[] extraHeaders)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Supabase social config is missing. Launch the game from the signed-in launcher.");

        using UnityWebRequest request = new UnityWebRequest($"{supabaseUrl}{path}", method);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("apikey", anonKey);
        request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

        foreach ((string name, string value) in extraHeaders)
            request.SetRequestHeader(name, value);

        if (body != null)
        {
            string json = JsonConvert.SerializeObject(body);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.SetRequestHeader("Content-Type", "application/json");
        }

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler?.text;
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response)
                ? request.error
                : response);
        }

        return request.downloadHandler?.text ?? string.Empty;
    }

    private async Task<Dictionary<string, ProfileRow>> FetchProfilesByIdsAsync(List<string> ids)
    {
        var result = new Dictionary<string, ProfileRow>(StringComparer.OrdinalIgnoreCase);
        if (ids == null || ids.Count == 0)
            return result;

        string joinedIds = string.Join(",", ids.FindAll(id => !string.IsNullOrWhiteSpace(id)));
        if (string.IsNullOrWhiteSpace(joinedIds))
            return result;

        string path = $"/rest/v1/profiles?select=id,display_name&id=in.({joinedIds})";
        string json = await SendAsync("GET", path);
        List<ProfileRow> rows = JsonConvert.DeserializeObject<List<ProfileRow>>(json) ?? new List<ProfileRow>();

        foreach (ProfileRow row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Id))
                result[row.Id] = row;
        }

        return result;
    }

    private static FriendProfile ToFriendProfile(ProfileRow row)
    {
        return new FriendProfile
        {
            UserId = row.Id,
            Username = row.DisplayName ?? row.Email ?? "Player",
            DisplayName = row.DisplayName ?? row.Email ?? "Player",
            Level = 1,
            IsOnline = true
        };
    }

    private static long ParseUnixSeconds(string timestamp)
    {
        return DateTimeOffset.TryParse(timestamp, out DateTimeOffset parsed)
            ? parsed.ToUnixTimeSeconds()
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private sealed class ProfileRow
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("email")] public string Email { get; set; }
        [JsonProperty("display_name")] public string DisplayName { get; set; }
        [JsonProperty("updated_at")] public string UpdatedAt { get; set; }
    }

    private sealed class FriendLinkRow
    {
        [JsonProperty("owner_id")] public string OwnerId { get; set; }
        [JsonProperty("friend_id")] public string FriendId { get; set; }
    }

    private sealed class LobbyInviteRow
    {
        [JsonProperty("sender_id")] public string SenderId { get; set; }
        [JsonProperty("recipient_id")] public string RecipientId { get; set; }
        [JsonProperty("relay_join_code")] public string RelayJoinCode { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
    }

    private sealed class LobbyInviteWithSenderRow
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("sender_id")] public string SenderId { get; set; }
        [JsonProperty("relay_join_code")] public string RelayJoinCode { get; set; }
        [JsonProperty("created_at")] public string CreatedAt { get; set; }
    }
}
