using System.Text.RegularExpressions;

public static class ParseHelper
{
    public static int IndexOfNth(this string source, char val, int nth = 0)
    {
        int index = source.IndexOf(val);

        for (int i = 0; i < nth; ++i)
        {
            if (index == -1)
                return -1;

            index = source.IndexOf(val, index + 1);
        }

        return index;
    }

    private static Regex symbolRegex = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
    public static bool CheckNameRegex(string displayName)
    {
        // If unusual characters/symbols are present in user's displayName then use the actual login name instead (login name is always lowercase)
        // This is useful because most fonts do not support those characters
        //
        // Allowed characters are: a-z, A-Z, 0-9, _

        // True = symbols detected
        return symbolRegex.IsMatch(displayName);
    }

    public static IRCTags ParseTags(string tagString, bool parseBadges, bool parseTwitchEmotes)
    {
        IRCTags tags = new IRCTags();
        string[] split = tagString.Split(';');

        // Loop through tags
        for (int i = 0; i < split.Length; ++i)
        {
            string value = split[i].Substring(split[i].IndexOf('=') + 1);

            if (value.Length <= 0) // Ignore empty tags
                continue;

            // Find the tags needed
            switch (split[i].Substring(0, split[i].IndexOf('=')))
            {
                case "badges":
                    if (parseBadges)
                        tags.badges = ParseBadges(value.Split(','));
                    continue;

                case "color":
                    tags.colorHex = value;
                    continue;

                case "display-name":
                    tags.displayName = value;
                    continue;

                case "emotes":
                    if (parseTwitchEmotes)
                        tags.emotes.AddRange(ParseTwitchEmotes(value.Split('/')));
                    continue;

                case "room-id": // room-id = channelId
                    tags.channelId = value;
                    continue;

                case "user-id":
                    tags.userId = value;
                    continue;
            }
        }

        return tags;
    }

    public static string ParseLoginName(string ircString)
    {
        return ircString.Substring(1, ircString.IndexOf('!') - 1);
    }

    public static string ParseChannel(string ircString)
    {
        string channel = ircString.Substring(ircString.IndexOf('#') + 1);

        int index = channel.IndexOf(' ');
        if (index == -1)
            return channel;
        else
            // Further parsing (needed for PRIVMSG for example)
            return channel.Substring(0, index);
    }

    public static string ParseMessage(string ircString)
    {
        return ircString.Substring(ircString.IndexOfNth(' ', 2) + 2);
    }

    public static ChatterEmote[] ParseTwitchEmotes(string[] splitEmotes)
    {
        ChatterEmote[] emotes = new ChatterEmote[splitEmotes.Length];

        for (int i = 0; i < splitEmotes.Length; ++i)
        {
            string e = splitEmotes[i];

            string[] indexes = e.Substring(e.IndexOf(':') + 1).Length > 0 ? e.Substring(e.IndexOf(':') + 1).Split(',') : new string[0];

            ChatterEmote.Index[] ind = new ChatterEmote.Index[indexes.Length];

            for (int j = 0; j < ind.Length; ++j)
            {
                ind[j].startIndex = int.Parse(indexes[j].Substring(0, indexes[j].IndexOf('-')));
                ind[j].endIndex = int.Parse(indexes[j].Substring(indexes[j].IndexOf('-') + 1));
            }

            emotes[i] = new ChatterEmote()
            {
                id = e.Substring(0, e.IndexOf(':')),
                indexes = ind
            };
        }

        return emotes;
    }

    public static ChatterBadge[] ParseBadges(string[] splitBadges)
    {
        ChatterBadge[] badges = new ChatterBadge[splitBadges.Length];

        for (int i = 0; i < splitBadges.Length; ++i)
        {
            string s = splitBadges[i];

            badges[i].id = s.Substring(0, s.IndexOf('/'));
            badges[i].version = s.Substring(s.IndexOf('/') + 1);
        }

        return badges;
    }
}
