using System;
using UnityEngine;

using Random = System.Random;

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

    public static class ChatColors
    {
        // These are taken from https://discuss.dev.twitch.tv/t/default-user-color-in-chat/385
        public static string[] defaultNameColors = new string[]
        {
            "#FF0000", // Red
            "#00FF00", // Green
            "#0000FF", // Blue
            "#B22222", // Firebrick
            "#FF7F50", // Coral
            "#9ACD32", // Yellow Green
            "#FF4500", // Orange Red
            "#2E8B57", // Sea Green
            "#DAA520", // Goldenrod
            "#D2691E", // Chocolate
            "#5F9EA0", // Cadet Blue
            "#1E90FF", // Dodger Blue
            "#FF69B4", // Hot Pink
            "#8A2BE2", // Blue Violet
            "#00FF7F", // Spring Green
        };

        /// <summary>
        /// <para>Gets a random name color based on the chatter's login (+ session random seed)</para>
        /// <para>This is similar to what native Twitch chat does</para>
        /// </summary>
        public static string GetRandomNameColor(int sessionRandom, string login)
        {
            int n = sessionRandom + login[0] + login[login.Length - 1];
            return defaultNameColors[n % defaultNameColors.Length];
        }

        public static float grayscaleLow = 0.3f;
        public static float grayscaleHigh = 1.0f;
        /// <summary>
        /// <para>Normalizes the color if needed to make it more readable</para>
        /// <para>Very bright colors are darkened, very dark colors are brightened</para>
        /// <para>This is similar to what native Twitch chat does</para>
        /// </summary>
        public static Color NormalizeColor(Color color)
        {
            // Too dark -> make brighter
            if (color.grayscale < grayscaleLow)
            {
                float delta = grayscaleLow - color.grayscale;
                return new Color(color.r + delta, color.g + delta, color.b + delta);
            }

            // Too bright -> make darker
            if (color.grayscale > grayscaleHigh)
            {
                float delta = grayscaleHigh - color.grayscale;
                return new Color(color.r + delta, color.g + delta, color.b + delta);
            }

            return color;
        }
    }
}