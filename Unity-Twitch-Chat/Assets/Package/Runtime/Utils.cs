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
        public static string[] defaultNameColors = new string[]
        {
            "#FFFFFF",
            "#000000",
            "#FF0000"
        };

        public static string GetRandomNameColor(Random rnd)
        {
            return defaultNameColors[rnd.Next(0, defaultNameColors.Length)];
        }


        public static float grayscaleLow = 0.3f;
        public static float grayscaleHigh = 0.9f;

        /// <summary>
        /// <para>Normalizes the color in a similar manner to Native Twitch chat</para>
        /// <para>Very bright colors are darkened, very dark colors are brightened to make them more readable</para>
        /// </summary>
        public static Color NormalizeColor(Color original)
        {
            if (original.grayscale == 0)
            {
                return new Color(grayscaleLow, grayscaleLow, grayscaleLow);
            }

            if (original.grayscale < grayscaleLow)
            {
                return new Color(original.r * 2f, original.g * 2f, original.b * 2f);
            }

            if (original.grayscale > grayscaleHigh)
            {
                return new Color(original.r / 2f, original.g / 2f, original.b / 2f);
            }

            return original;
        }
    }
}