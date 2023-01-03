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

        /// <summary>
        /// Returns whether the message contain a given emote.
        /// </summary>
        public bool ContainsEmote(string emote) => tags.ContainsEmote(emote);

        /// <summary>
        /// Returns whether the message has a given badge.
        /// </summary>
        public bool HasBadge(string badge) => tags.HasBadge(badge);
    }

}