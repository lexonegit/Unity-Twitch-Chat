using System.Text;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;

namespace Lexonegit.UnityTwitchChat
{

    public partial class TwitchIRC
    {
        private byte[] inputBuffer;
        private char[] chars;
        private StringBuilder currentString = new StringBuilder();
        private Decoder decoder = Encoding.UTF8.GetDecoder();

        /// <summary>
        /// The IRC input process which will run on the receive thread.
        /// </summary>
        private void IRCInputProc()
        {
            Debug.Log("IRCInput Thread (Receive) started");

            // get the Socket from the TcpClient
            Socket socket = client.Client;
            currentString.Clear();
            inputBuffer = new byte[readBufferSize];
            chars = new char[readBufferSize];

            while (connected)
            {
                // check if new data is available
                while (socket.Available > 0)
                {
                    // receive the data
                    var bytesReceived = socket.Receive(inputBuffer);

                    // decode the data into text
                    var charCount = decoder.GetChars(inputBuffer, 0, bytesReceived, chars, 0);

                    // iterate through the received characters
                    for (var i = 0; i < charCount; i++)
                    {
                        // if the character is a linebreak...
                        if (chars[i] == '\n' || chars[i] == '\r')
                        {
                            // handle a string, if there is one
                            if (currentString.Length > 0)
                            {
                                HandleLine(currentString.ToString());
                                currentString.Clear();
                            }
                            continue;
                        }
                        else
                        {
                            // append non-linebreak characters to the current string
                            currentString.Append(chars[i]);
                        }
                    }
                }

                // sleep for a short period
                Thread.Sleep(readInterval);
            }

            Debug.LogWarning("IRCInput Thread (Receive) exited");
        }

        /// <summary>
        /// Handle a completed line from the network stream.
        /// </summary>
        private void HandleLine(string raw)
        {
            if (settings.debugIRC)
                Debug.Log("<color=#005ae0><b>[IRC INPUT]</b></color> " + raw);

            string ircString = raw;
            string tagString = string.Empty;

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
                    case "353": // = Successful channel join
                    case "001": // = Successful IRC connection
                        HandleRPL(type);
                        break;
                }
            }

            // Respond to PING messages
            if (raw.StartsWith("PING"))
                SendCommand("PONG :tmi.twitch.tv", true);
        }


        /// <summary>
        /// Handle an RPL message from the server.
        /// </summary>
        private void HandleRPL(string type)
        {
            switch (type)
            {
                case "001":
                    SendCommand("JOIN #" + twitchDetails.channel.ToLower(), true);
                    ConnectionStateAlert(StatusType.Success, "Connected to Twitch IRC. Now joining channel: " + twitchDetails.channel + "...", 100);
                    break;
                case "353":
                    Debug.Log("<color=#bd2881><b>[JOIN]</b></color> Joined channel: " + twitchDetails.channel + " successfully");
                    break;
            }
        }

        /// <summary>
        /// Handle a PRIVMSG command form the server.
        /// </summary>
        private void HandlePRIVMSG(string ircString, string tagString)
        {
            // Parse PRIVMSG
            IRCPrivmsg privmsg = new IRCPrivmsg(
                ParseHelper.ParseLoginName(ircString),
                ParseHelper.ParseChannel(ircString),
                ParseHelper.ParseMessage(ircString)
            );

            // Parse Tags
            IRCTags tags = ParseHelper.ParseTags(tagString, settings.parseBadges, settings.parseTwitchEmotes);

            // Sort emotes to match emote order with the chat message (compares emote indexes)
            if (tags.emotes.Count > 0)
                tags.emotes.Sort((a, b) => 1 * a.indexes[0].startIndex.CompareTo(b.indexes[0].startIndex));

            // Send chatter object to listeners
            // Invoke in main thread
            taskQueue.Enqueue(() => newChatMessageEvent.Invoke(new Chatter(privmsg, tags)));
        }

        /// <summary>
        /// Handle a USERSTATE command form the server.
        /// </summary>
        private void HandleUSERSTATE(string ircString, string tagString)
        {
            // Parse USERSTATE
            IRCUserstate userstate = new IRCUserstate(ParseHelper.ParseChannel(ircString));

            // Parse Tags
            IRCTags tags = ParseHelper.ParseTags(tagString, settings.parseBadges, settings.parseTwitchEmotes);

            clientChatter = new Chatter(userstate, tags);
        }
    }

}