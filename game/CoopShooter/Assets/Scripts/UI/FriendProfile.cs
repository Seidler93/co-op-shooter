using System;

[Serializable]
public class FriendProfile
{
    public string UserId;
    public string Username;
    public string DisplayName;
    public int Level = 1;
    public bool IsOnline;

    public string BestName => string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName;
}
