using UnityEngine;

namespace Incredulous.Twitch
{

    [System.Serializable]
    public class Chatter
    {
        public Chatter(string login, string channel, string message, IRCTags tags)
        {
            this.login = login;
            this.channel = channel;
            this.message = message;
            this.tags = tags;
        }

        public string login, channel, message;
        public IRCTags tags = null;

        /// <summary>
        /// Get RGBA color using HEX color code
        /// </summary>
        public Color GetRGBAColor()
        {
            if (ColorUtility.TryParseHtmlString(tags.colorHex, out Color color))
                return color;
            else
                //Return default white if parsing fails for some reason
                return new Color(1, 1, 1, 1);
        }

        /// <summary>
        /// Returns true if name is "font-safe" meaning that it only contains characters: a-z, A-Z, 0-9, _
        /// </summary>
        public bool IsDisplayNameFontSafe()
        {
            return ParseHelper.CheckNameRegex(tags.displayName);
        }

        public bool MessageContainsEmote(string emoteId)
        {
            foreach (ChatterEmote e in tags.emotes)
            {
                if (e.id == emoteId)
                    return true;
            }

            return false;
        }

        public bool HasBadge(string badgeName)
        {
            foreach (ChatterBadge b in tags.badges)
            {
                if (b.id == badgeName)
                    return true;
            }

            return false;
        }
    }

}