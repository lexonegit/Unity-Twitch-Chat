using UnityEngine;
using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace Lexone.UnityTwitchChat
{
    internal partial class TwitchConnection
    {
        // Used to generate session based random colors for undefined users
        private int sessionRandom = DateTime.Now.Second;

        private void ReadThreadLoop()
        {
            if (showThreadDebug)
                Debug.Log($"{Tags.thread} Read thread started");

            using (NetworkStream stream = tcpClient.GetStream())
            {
                byte[] readBuffer = new byte[readBufferSize];
                StringBuilder currentLine = new StringBuilder();
                Decoder decoder = Encoding.UTF8.GetDecoder();

                // Temporary solution(?):
                // Certain multi-byte UTF-8 characters can result in overflowing (special characters)
                // So we add a little extra space to the chars array just to be safe
                char[] chars = new char[readBufferSize + Mathf.Clamp(readBufferSize / 4, 1, 32)];

                while (ThreadsRunning)
                {
                    // Check if the connection is still alive
                    if (!CheckConnection(tcpClient.Client))
                    {
                        alertQueue.Enqueue(IRCReply.CONNECTION_INTERRUPTED);
                        return;
                    }

                    while (stream.DataAvailable)
                    {
                        // Read the bytes from the stream
                        int bytesReceived = stream.Read(readBuffer, 0, readBufferSize);

                        // Decode the bytes to chars
                        int charsDecoded = decoder.GetChars(readBuffer, 0, bytesReceived, chars, 0);

                        for (int i = 0; i < charsDecoded; ++i)
                        {
                            // Character is a linebreak -> We have a complete line
                            if (chars[i] == '\n' || chars[i] == '\r')
                            {
                                // If the line is not empty, handle it
                                if (currentLine.Length > 0)
                                {
                                    HandleRawLine(currentLine.ToString());
                                    currentLine.Clear();
                                }
                            }
                            else
                            {
                                // Append the character to the current line
                                currentLine.Append(chars[i]);
                            }
                        }
                    }

                    // Sleep to prevent high CPU usage
                    Thread.Sleep(readInterval);
                }
            }

            if (showThreadDebug)
                Debug.Log($"{Tags.thread} Read thread stopped");
        }

        private bool CheckConnection(Socket socket)
        {
            bool poll = socket.Poll(1000, SelectMode.SelectRead);
            bool available = (socket.Available == 0);

            if ((poll && available) || !socket.Connected)
                return false;
            else
                return true;
        }


        private void HandleRawLine(string raw)
        {
            if (showIRCDebug)
                Debug.Log($"{Tags.read} {raw}");

            string ircString = raw;
            string tagString = string.Empty;

            // Parsing the raw IRC lines...

            if (raw[0] == '@')
            {
                int ind = raw.IndexOf(' ');
                tagString = raw.Substring(0, ind);
                ircString = raw.Substring(ind).TrimStart();
            }

            if (ircString[0] == ':')
            {
                string type = ircString.Substring(ircString.IndexOf(' ')).TrimStart();
                type = type.Substring(0, type.IndexOf(' '));

                switch (type)
                {
                    case "PRIVMSG": // = Chat message
                        HandlePRIVMSG(ircString, tagString);
                        break;
                    case "USERSTATE": // = Userstate
                        HandleUSERSTATE(ircString, tagString);
                        break;
                    case "NOTICE": // = Notice
                        HandleNOTICE(ircString, tagString);
                        break;

                    // RPL messages
                    case "353": // = Successful channel join
                    case "001": // = Successful IRC connection
                        HandleRPL(type);
                        break;
                }
            }

            // Respond to PING messages with PONG
            if (raw.StartsWith("PING"))
                Pong();

            // Alert when PONG messages are received
            if (raw.StartsWith(":tmi.twitch.tv PONG"))
                alertQueue.Enqueue(IRCReply.PONG_RECEIVED);
        }

        /// <summary>
        /// Handle a PRIVMSG message
        /// </summary>
        private void HandlePRIVMSG(string ircString, string tagString)
        {
            var login = ParseHelper.ParseLoginName(ircString);
            var channel = ParseHelper.ParseChannel(ircString);
            var message = ParseHelper.ParseMessage(ircString);
            var tags = ParseHelper.ParseTags(tagString);

            // Not all users have set their Twitch name color, so we need to check for that
            if (tags.colorHex.Length <= 0)
                tags.colorHex = useRandomColorForUndefined
                    ? ChatColors.GetRandomNameColor(sessionRandom, login)
                    : "#FFFFFF";

            // Sort emotes by startIndex to match emote order in the actual chat message
            if (tags.emotes.Length > 0)
            {
                Array.Sort(tags.emotes, (a, b) =>
                    a.indexes[0].startIndex.CompareTo(b.indexes[0].startIndex));
            }

            // Queue new chatter object
            chatterQueue.Enqueue(new Chatter(login, channel, message, tags));
        }

        /// <summary>
        /// Handle a USERSTATE message
        /// </summary>
        private void HandleUSERSTATE(string ircString, string tagString)
        {
            var tags = ParseHelper.ParseTags(tagString);
            ClientUserTags = tags;
            UpdateRateLimits(); // Update rate limits based on client tags
        }

        /// <summary>
        /// Handle a NOTICE message
        /// </summary>
        private void HandleNOTICE(string ircString, string tagString)
        {
            if (ircString.Contains(":Login authentication failed"))
            {
                alertQueue.Enqueue(IRCReply.BAD_LOGIN);
            }
        }

        /// <summary>
        /// Handle an RPL message
        /// </summary>
        private void HandleRPL(string type)
        {
            switch (type)
            {
                case "001":
                    alertQueue.Enqueue(IRCReply.CONNECTED_TO_SERVER);

                    if (joinChannelOnStart)
                        IRC.Instance.JoinChannel(channel);
                        
                    break;
                case "353":
                    alertQueue.Enqueue(IRCReply.JOINED_CHANNEL);
                    break;
            }
        }
    }
}
