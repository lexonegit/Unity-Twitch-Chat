
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
        /// <param name="normalize">Should the name color be normalized, if needed?</param>
        /// </summary>
        public Color GetNameColor(bool normalize = true)
        {
            if (ColorUtility.TryParseHtmlString(tags.colorHex, out Color color))
            {
                if (normalize)
                    return ChatColors.NormalizeColor(color);
                else
                    return color;
            }
            else
                return Color.white; // Parsing failed somehow, return default white
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
        public bool HasBadge(string badgeName) => tags.HasBadge(badgeName);
    }
}