using UnityEngine;
using System.Net.Sockets;
using System.Text;

namespace Lexone.UnityTwitchChat
{
    public static class Extensions
    {
        public static void WriteLine(this NetworkStream stream, string output, bool showDebug = false)
        {
            if (showDebug)
                Debug.Log($"{Tags.write} {output}");
                
            byte[] bytes = Encoding.UTF8.GetBytes(output);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte((byte)'\r');
            stream.WriteByte((byte)'\n');
            stream.Flush();
        }

        /// <summary>
        /// Returns a string description for the ConnectionAlert enum.
        /// </summary>
        public static string GetDescription(this IRCReply alert)
        {
            switch (alert)
            {
                case IRCReply.CONNECTED_TO_SERVER:
                    return "Connected to IRC";
                case IRCReply.PONG_RECEIVED:
                    return "Pong!";
                case IRCReply.JOINED_CHANNEL:
                    return "Joined channel";
                case IRCReply.MISSING_LOGIN_INFO:
                    return "Missing login information (OAuth or username)";
                case IRCReply.BAD_LOGIN:
                    return "Login failed";
                case IRCReply.CONNECTION_INTERRUPTED:
                    return "Connection to IRC interrupted";
                case IRCReply.NO_CONNECTION:
                    return "Connection to IRC failed";
                default:
                    return "Unknown alert";
            }
        }
    }
}
