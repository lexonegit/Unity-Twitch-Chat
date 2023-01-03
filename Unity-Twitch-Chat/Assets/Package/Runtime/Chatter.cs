
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
        /// <para>Returns the RGBA color of the chatter's name (tags.colorHex)</para>
        /// <para>Note that the color will be white if the user has not set their name color</para>
        /// </summary>
        public Color GetNameColor()
        {
            if (
                tags.colorHex.Length > 0 &&
                ColorUtility.TryParseHtmlString(tags.colorHex, out Color color)
            )
                return color;
            else
                // User has not set a name color, or parsing failed somehow -> return white
                // TODO: Return a random color instead (Native Twitch chat does this, although it's not fully random)
                return new Color(1, 1, 1, 1);
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
        /// <para>Returns true if the chatter's message contains a given emote (by emote ID)</para>
        /// <para>You can find emote IDs by using the Twitch API, or 3rd party sites</para>
        /// </summary>
        public bool ContainsEmote(string emoteId) => tags.ContainsEmote(emoteId);

        /// <summary>
        /// Returns true if the chatter has a given badge.
        /// </summary>
        public bool HasBadge(string badge) => tags.HasBadge(badge);
    }
}