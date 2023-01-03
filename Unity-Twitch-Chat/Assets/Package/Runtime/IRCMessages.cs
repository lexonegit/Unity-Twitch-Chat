using System.Collections.Generic;

namespace UnityTwitchChat
{
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
        public string colorHex = string.Empty;
        public string displayName = string.Empty;
        public string channelId = string.Empty;
        public string userId = string.Empty;

        public ChatterBadge[] badges = new ChatterBadge[0];
        public List<ChatterEmote> emotes = new List<ChatterEmote>();

        public bool ContainsEmote(string emoteId)
        {
            foreach (ChatterEmote e in emotes)
            {
                if (e.id == emoteId)
                    return true;
            }

            return false;
        }

        public bool HasBadge(string badge)
        {
            foreach (ChatterBadge b in badges)
            {
                if (b.id == badge)
                    return true;
            }

            return false;
        }
    }
}