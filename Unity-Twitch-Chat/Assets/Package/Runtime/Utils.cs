namespace Lexone.UnityTwitchChat
{
    /// <summary>
    /// https://www.ietf.org/rfc/rfc1459.txt (starting on page 42 - Replies)
    /// </summary>
    public enum IRCReply
    {
        // Twitch IRC specific codes
        CONNECTED_TO_SERVER = 001,
        PONG_RECEIVED = 007,

        // Command codes
        JOINED_CHANNEL = 353,

        // Error codes
        MISSING_LOGIN_INFO = 431,
        BAD_LOGIN = 464,
        CONNECTION_INTERRUPTED = 498,
        NO_CONNECTION = 499,
    }

    public static class Tags
    {
        public static string read = "<color=#00ff00><b>[IRC READ]</b></color>";
        public static string write = "<color=#5274ff><b>[IRC WRITE]</b></color>";
        public static string thread = "<color=#ff5252><b>[THREAD]</b></color>";
        public static string alert = "<color=#ffae00><b>[ALERT]</b></color>";
    }
}