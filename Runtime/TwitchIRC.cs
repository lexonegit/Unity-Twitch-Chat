using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Lexonegit.UnityTwitchChat
{

    // https://github.com/lexonegit/Unity-Twitch-Chat
    public class TwitchIRC : MonoBehaviour
    {
        [HideInInspector] public class NewChatMessageEvent : UnityEngine.Events.UnityEvent<Chatter> { }
        [HideInInspector] public class StatusEvent : UnityEngine.Events.UnityEvent<StatusType, string, int> { }

        // Events
        public NewChatMessageEvent newChatMessageEvent = new NewChatMessageEvent(); // New chat messages
        public StatusEvent statusEvent = new StatusEvent(); // Connection events

        private TcpClient client;
        private NetworkStream stream;

        public string ircAddress = "irc.chat.twitch.tv";
        public int port = 6667;

        /// <summary>
        /// The number of milliseconds between each time the input thread checks for new inputs.
        /// </summary>
        public int readInterval = 100;

        /// <summary>
        /// The number of milliseconds between each time the output thread checks its queues.
        /// </summary>
        public int writeInterval = 500;

        /// <summary>
        /// The number of milliseconds Twitch requires between IRC writes.
        /// </summary>
        private const int twitchRateLimitSleepTime = 1750;

        public TwitchDetails twitchDetails;
        public Settings settings;

        [Header("Client chatter object")]
        [Tooltip("Contains some information about the client user (OAuth)")] public Chatter clientChatter;

        private int _connected = 0;
        private bool connected
        {
            get => _connected == 1;
            set
            {
                Interlocked.Exchange(ref _connected, value ? 1 : 0);
            }
        }


        private Thread outputThread = null;
        private Thread inputThread = null;

        private ConcurrentQueue<System.Action> taskQueue = new ConcurrentQueue<System.Action>();

        [System.Serializable]
        public class TwitchDetails
        {
            public string oauth = string.Empty;
            public string nick = string.Empty;
            public string channel = string.Empty;
        }

        [System.Serializable]
        public class Settings
        {
            public bool connectOnStart = true;
            public bool parseBadges = true;
            public bool parseTwitchEmotes = true;
            [Space(12f)]
            public bool debugIRC = true;
        }

        #region Unity MonoBehaviour functions
        private void Start()
        {
            if (settings.connectOnStart)
                StartCoroutine(PrepareConnection());
        }

        private void Update()
        {
            while (taskQueue.Count > 0)
            {
                if (taskQueue.TryDequeue(out var task))
                    task.Invoke();
            }
        }

        private void OnDestroy()
        {
            BlockingDisconnect();
        }

        private void OnDisable()
        {
            BlockingDisconnect();
        }
        #endregion

        [ContextMenu("Connect IRC")]
        public void IRC_Connect()
        {
            StartCoroutine(PrepareConnection());
        }

        [ContextMenu("Disconnect IRC")]
        public void IRC_Disconnect()
        {
            StartCoroutine(Disconnect());
        }

        [ContextMenu("Send PING")]
        private void SendPing()
        {
            SendCommand("PING :tmi.twitch.tv", true);
        }

        private IEnumerator PrepareConnection()
        {
            // End any current connection
            if (connected)
                yield return StartCoroutine(Disconnect());

            // Wait for previous threads to close (if there are any)
            while (inputThread != null && inputThread.IsAlive)
                yield return null;
            while (outputThread != null && inputThread.IsAlive)
                yield return null;

            // Verify that login information has been provided
            if (twitchDetails.oauth.Length <= 0 || twitchDetails.nick.Length <= 0 || twitchDetails.channel.Length <= 0)
            {
                ConnectionStateAlert(StatusType.Error, "Missing required details! Check your Twitch details.");
                yield break;
            }

            // Fix formatting (twitchapps.com)
            if (twitchDetails.oauth.StartsWith("oauth:"))
                twitchDetails.oauth = twitchDetails.oauth.Substring(6);

            ConnectIRC();
        }

        private void ConnectIRC()
        {
            // Connect to Twitch IRC
            client = new TcpClient(ircAddress, port);
            stream = client.GetStream();

            if (!client.Connected)
            {
                ConnectionStateAlert(StatusType.Error, "Failed connecting to Twitch IRC.");
                return;
            }

            stream.WriteLine("PASS oauth:" + twitchDetails.oauth.ToLower());
            stream.WriteLine("NICK " + twitchDetails.nick.ToLower());
            stream.WriteLine("CAP REQ :twitch.tv/tags twitch.tv/commands");

            connected = true;

            // Clear the task queue (NOTE: ConcurrentQueue does not contain a Clear method in .NET Standard 2.0)
            while (!taskQueue.IsEmpty)
                taskQueue.TryDequeue(out var discard);

            // Clear the output queue
            while (!outputQueue.IsEmpty)
                outputQueue.TryDequeue(out var discard);

            // Initialize threads
            inputThread = new Thread(() => IRCInputProc(client.Client, 128));
            outputThread = new Thread(() => IRCOutputProc());

            // Start threads
            inputThread.Start();
            outputThread.Start();
        }

        private IEnumerator Disconnect(bool reconnect = false)
        {
            if (!connected)
                yield break;

            // Instruct the threads to stop running
            connected = false;

            // Wait for threads to close
            while (inputThread.IsAlive)
                yield return null;
            while (outputThread.IsAlive)
                yield return null;

            // Close the TcpClient
            client.Close();

            Debug.LogWarning("Disconnected from Twitch IRC");

            if (reconnect)
                StartCoroutine(PrepareConnection());
        }

        private void BlockingDisconnect()
        {
            if (!connected)
                return;

            // Instruct the threads to stop running
            connected = false;

            // Wait for threads to close
            inputThread.Join();
            outputThread.Join();

            // Close the TcpClient
            client.Close();

            Debug.LogWarning("Disconnected from Twitch IRC");
        }

        private byte[] inputBuffer;
        private char[] chars;

        private StringBuilder currentString = new StringBuilder();
        private Decoder decoder = Encoding.UTF8.GetDecoder();

        private void IRCInputProc(Socket socket, int bufferSize)
        {
            Debug.Log("IRCInput Thread (Receive) started");

            currentString.Clear();
            inputBuffer = new byte[bufferSize];
            chars = new char[bufferSize];

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
                        HandleRPL(type);
                        break;
                    case "001": // = Successful IRC connection
                        HandleRPL(type);
                        break;
                }
            }

            // Respond to PING messages
            if (raw.StartsWith("PING"))
                SendCommand("PONG :tmi.twitch.tv", true);
        }

        private ConcurrentQueue<string> priorityOutputQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();

        private void IRCOutputProc()
        {
            Debug.Log("IRCOutput Thread (Send) started");

            //Read loop
            while (connected)
            {
                int sleepTime = writeInterval;

                if (priorityOutputQueue.Count > 0)
                {
                    // Send next output from priorityOutputQueue
                    if (priorityOutputQueue.TryDequeue(out var output))
                    {
                        stream.WriteLine(output, settings.debugIRC);
                        sleepTime = twitchRateLimitSleepTime;
                    }
                }
                else if (outputQueue.Count > 0)
                {
                    // Send next output from outputQueue
                    if (outputQueue.TryDequeue(out var output))
                    {
                        stream.WriteLine(output, settings.debugIRC);
                        sleepTime = twitchRateLimitSleepTime;
                    }
                }

                // Sleep for a short while before checking again
                Thread.Sleep(sleepTime);
            }

            Debug.LogWarning("IRCOutput Thread (Send) exited");
        }

        private void HandleRPL(string type)
        {
            switch (type)
            {
                case "001":
                    SendCommand("JOIN #" + twitchDetails.channel.ToLower());
                    ConnectionStateAlert(StatusType.Success, "Connected to Twitch IRC. Now joining channel: " + twitchDetails.channel + "...", 100);
                    break;
                case "353":
                    Debug.Log("<color=#bd2881><b>[JOIN]</b></color> Joined channel: " + twitchDetails.channel + " successfully");
                    break;
            }
        }

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
        private void HandleUSERSTATE(string ircString, string tagString)
        {
            // Parse USERSTATE
            IRCUserstate userstate = new IRCUserstate(ParseHelper.ParseChannel(ircString));

            // Parse Tags
            IRCTags tags = ParseHelper.ParseTags(tagString, settings.parseBadges, settings.parseTwitchEmotes);

            clientChatter = new Chatter(userstate, tags);
        }

        /// <summary>
        /// Queues a command to be sent to the IRC server. All prioritzed commands will be sent before non-prioritized commands.
        /// </summary>
        public void SendCommand(string command, bool prioritized = false)
        {
            // Place command in respective queue
            if (prioritized)
                priorityOutputQueue.Enqueue(command);
            else
                outputQueue.Enqueue(command);
        }

        /// <summary>
        /// Sends a chat message
        /// </summary>
        public void SendChatMessage(string message)
        {
            if (message.Length <= 0) // Message can't be empty
                return;

            outputQueue.Enqueue("PRIVMSG #" + twitchDetails.channel + " :" + message); // Place message in queue
        }

        public enum StatusType { Normal, Success, Error };
        public void ConnectionStateAlert(StatusType state, string message, int percentage = 0)
        {
            switch (state)
            {
                case StatusType.Error:
                    Debug.LogError("<color=red><b>[ERROR]</b></color>: " + message);
                    break;

                case StatusType.Normal:
                    Debug.Log("<color=#0018a1><b>[STATUS]</b></color>: <b>" + percentage + "%</b> " + message);
                    break;

                case StatusType.Success:
                    Debug.Log("<color=#0ea300><b>[SUCCESS]</b></color>: <b>" + percentage + "%</b> " + message);
                    break;
            }

            // Send status event to other listeners
            statusEvent.Invoke(state, message, percentage);
        }
    }

}