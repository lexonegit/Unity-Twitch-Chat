using System.Collections.Generic;

[System.Serializable]
public struct ChatterEmote
{
    [System.Serializable]
    public struct Index
    {
        public int startIndex, endIndex;
    }

    public string id;
    public Index[] indexes;
}

[System.Serializable]
public struct ChatterBadge
{
    public string id;
    public string version;
}

[System.Serializable]
public class IRCTags
{
    public string colorHex = "#FFFFFF";
    public string displayName = string.Empty;
    public string channelId = string.Empty;
    public string userId = string.Empty;

    public ChatterBadge[] badges = new ChatterBadge[0];
    public List<ChatterEmote> emotes = new List<ChatterEmote>();
}

public class IRCPrivmsg
{
    public IRCPrivmsg(string Login, string Channel, string Message)
    {
        login = Login;
        channel = Channel;
        message = Message;
    }

    public string login, channel, message;
}

public class IRCUserstate
{
    public IRCUserstate(string Channel)
    {
        channel = Channel;
    }

    public string channel;
}