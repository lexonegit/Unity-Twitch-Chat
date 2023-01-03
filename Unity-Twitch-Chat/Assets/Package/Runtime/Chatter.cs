
using UnityEngine;

namespace Lexone.UnityTwitchChat
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
        /// Returns the RGBA color of the chatter's name (tags.colorHex)
        /// </summary>
        public Color GetNameColor()
        {
            if (
                tags.colorHex.Length > 0 &&
                ColorUtility.TryParseHtmlString(tags.colorHex, out Color color)
            )
                return color;
            else
                return new Color(1, 1, 1, 1); // Failed parsing color -> return white
        }

        /// <summary>
        /// <para>Returns true if displayName is "font-safe" 
        /// meaning that it only contains characters: a-z, A-Z, 0-9, _</para>
        /// <para>Useful because most fonts do not support unusual characters</para>
        /// </summary>
        public bool IsDisplayNameFontSafe()
        {
            return ParseHelper.CheckNameRegex(tags.displayName);
        }

        /// <summary>
        /// Returns true if the chatter's message contains a given emote (by emote ID)
        /// </summary>
        public bool ContainsEmote(string emoteId) => tags.ContainsEmote(emoteId);

        /// <summary>
        /// Returns true if the chatter has a given badge.
        /// </summary>
        public bool HasBadge(string badge) => tags.HasBadge(badge);
    }
}