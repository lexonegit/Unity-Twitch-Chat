using System.Collections.Generic;

namespace Incredulous.Twitch
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
        public string colorHex = "#FFFFFF";
        public string displayName = string.Empty;
        public string channelId = string.Empty;
        public string userId = string.Empty;

        public ChatterBadge[] badges = new ChatterBadge[0];
        public List<ChatterEmote> emotes = new List<ChatterEmote>();

        /// <summary>
        /// Returns whether the tags contain a given emote.
        /// </summary>
        public bool ContainsEmote(string emote)
        {
            foreach (ChatterEmote e in emotes)
            {
                if (e.id == emote)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether the tags contain a given badge.
        /// </summary>
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