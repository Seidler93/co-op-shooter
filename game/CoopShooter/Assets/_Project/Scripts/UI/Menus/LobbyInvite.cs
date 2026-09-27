using System;

[Serializable]
public class LobbyInvite
{
    public string InviteId;
    public string FromUserId;
    public string FromUsername;
    public string RelayJoinCode;
    public long CreatedAtUnixSeconds;

    public string SenderName => string.IsNullOrWhiteSpace(FromUsername) ? "Unknown player" : FromUsername;
}
