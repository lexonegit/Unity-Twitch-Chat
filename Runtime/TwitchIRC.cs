using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;

using System.Threading;
using UnityEngine;

namespace Incredulous.Twitch
{
    /// <summary>
    /// A class which represents a connection to the Twitch IRC API.
    /// </summary>
    public partial class TwitchIRC : MonoBehaviour
    {
        #region Inspector Values

        [Header("Server Information")]

        /// <summary>
        /// The server address.
        /// </summary>
        public string ircAddress = "irc.chat.twitch.tv";

        /// <summary>
        /// The server port.
        /// </summary>
        public int port = 6667;

        /// <summary>
        /// Twitch login details.
        /// </summary>
        public TwitchDetails twitchDetails;


        [Header("Read Information")]

        /// <summary>
        /// The number of milliseconds between each time the input thread checks for new inputs.
        /// </summary>
        public int readInterval = 100;

        /// <summary>
        /// The capacity of the read buffer. Smaller values consume less memory but require more cycles to retrieve data.
        /// </summary>
        public int readBufferSize = 128;


        [Header("Write Information")]

        /// <summary>
        /// The number of milliseconds between each time the output thread checks its queues.
        /// </summary>
        public int writeInterval = 500;


        [Header("Other Settings")]

        /// <summary>
        /// General settings for the Twitch client.
        /// </summary>
        public Settings settings;


        [Header("Client Chatter Object")]

        /// <summary>
        /// Contains some information about the client user (OAuth).
        /// </summary>
        public Chatter clientChatter;

        #endregion

        #region Events

        /// <summary>
        /// A delegate which handles a new chat message from the server.
        /// </summary>
        public delegate void ChatMessageEventHandler(Chatter chatter);

        /// <summary>
        /// A delegate which handles a status update from the Twitch IRC client.
        /// </summary>
        public delegate void StatusUpdateEventHandler(StatusType statusType, string message, int percent);

        /// <summary>
        /// An event which is triggered when a new chat message is received.
        /// </summary>
        public event ChatMessageEventHandler ChatMessageEvent;

        /// <summary>
        /// An event which is triggered when the connection status changes.
        /// </summary>
        public event StatusUpdateEventHandler StatusUpdateEvent;

        #endregion

        /// <summary>
        /// The number of milliseconds Twitch requires between IRC writes.
        /// </summary>
        private const int twitchRateLimitSleepTime = 1750;

        /// <summary>
        /// The TCP client.
        /// </summary>
        private TcpClient client;

        /// <summary>
        /// The thread which handles sending to the IRC server.
        /// </summary>
        private Thread sendThread = null;

        /// <summary>
        /// The thread which handles receiving from the IRC server.
        /// </summary>
        private Thread receiveThread = null;

        /// <summary>
        /// A thread safe queue which performs actions on the main thread.
        /// </summary>
        private ConcurrentQueue<System.Action> taskQueue = new ConcurrentQueue<System.Action>();

        /// <summary>
        /// Whether the client should currently be connected to Twitch.
        /// </summary>
        private bool connected
        {
            get => _connected == 1;
            set => Interlocked.Exchange(ref _connected, value ? 1 : 0);
        }
        private int _connected = 0;

        #region Unity MonoBehaviour functions

        private void Start()
        {
            if (settings.connectOnStart)
                StartCoroutine(ConnectCoroutine());
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

        /// <summary>
        /// Connect or reconnect to Twitch IRC.
        /// </summary>
        [ContextMenu("Connect IRC")]
        public void Connect()
        {
            StartCoroutine(ConnectCoroutine());
        }

        /// <summary>
        /// Disconnect from Twitch IRC.
        /// </summary>
        [ContextMenu("Disconnect IRC")]
        public void Disconnect()
        {
            StartCoroutine(DisconnectCoroutine());
        }

        /// <summary>
        /// Ping the Twitch server.
        /// </summary>
        [ContextMenu("Send PING")]
        private void SendPing()
        {
            SendCommand("PING :tmi.twitch.tv", true);
        }

        /// <summary>
        /// A coroutine which builds the connection to the Twitch IRC server and starts the send/receive threads.
        /// </summary>
        private IEnumerator ConnectCoroutine()
        {
            // End any current connection
            if (connected)
                yield return StartCoroutine(DisconnectCoroutine());

            // Wait for previous threads to close (if there are any)
            while (receiveThread != null && receiveThread.IsAlive)
                yield return null;
            while (sendThread != null && sendThread.IsAlive)
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

            // Connect to Twitch IRC
            client = new TcpClient(ircAddress, port);

            if (!client.Connected)
            {
                ConnectionStateAlert(StatusType.Error, "Failed connecting to Twitch IRC.");
                yield break;
            }
            connected = true;

            // Clear the task queue (NOTE: ConcurrentQueue does not contain a Clear method in .NET Standard 2.0)
            while (!taskQueue.IsEmpty)
                taskQueue.TryDequeue(out _);

            // Clear the output queue
            while (!outputQueue.IsEmpty)
                outputQueue.TryDequeue(out _);

            // Initialize threads
            receiveThread = new Thread(() => IRCInputProc());
            sendThread = new Thread(() => IRCOutputProc());
            connectionThread = new Thread(() => CheckSocketProc());

            // Start threads
            receiveThread.Start();
            sendThread.Start();
            connectionThread.Start();

            // Queue login commands
            SendCommand("PASS oauth:" + twitchDetails.oauth.ToLower(), true);
            SendCommand("NICK " + twitchDetails.nick.ToLower(), true);
            SendCommand("CAP REQ :twitch.tv/tags twitch.tv/commands", true);
        }

        /// <summary>
        /// A coroutine which instructs the send/receive threads to terminate, waits for them to end, then closes the TCP connection.
        /// </summary>
        private IEnumerator DisconnectCoroutine()
        {
            if (!connected)
                yield break;

            // Instruct the threads to stop running
            connected = false;

            // Wait for threads to close
            while (receiveThread.IsAlive)
                yield return null;
            while (sendThread.IsAlive)
                yield return null;
            while (connectionThread.IsAlive)
                yield return null;

            // Close the TcpClient
            client.Close();

            Debug.LogWarning("Disconnected from Twitch IRC");
        }

        /// <summary>
        /// Disconnect from Twitch IRC. <b>Blocks the main thread. Use carefully.</b>
        /// </summary>
        private void BlockingDisconnect()
        {
            if (!connected)
                return;

            // Instruct the threads to stop running
            connected = false;

            // Wait for threads to close
            receiveThread.Join();
            sendThread.Join();
            connectionThread.Join();

            // Close the TcpClient
            client.Close();

            Debug.LogWarning("Disconnected from Twitch IRC");
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
        /// Sends a chat message.
        /// </summary>
        public void SendChatMessage(string message)
        {
            if (message.Length <= 0) // Message can't be empty
                return;

            outputQueue.Enqueue("PRIVMSG #" + twitchDetails.channel + " :" + message); // Place message in queue
        }

        /// <summary>
        /// Status types for the Twitch IRC client.
        /// </summary>
        public enum StatusType { Normal, Success, Error };

        /// <summary>
        /// Sends a connection state alert to the console and any listeners.
        /// </summary>
        private void ConnectionStateAlert(StatusType state, string message, int percentage = 0)
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
            StatusUpdateEvent?.Invoke(state, message, percentage);
        }

        #region Subclasses

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
            public bool debugIRC = true;
        }

        #endregion

        private const int connectionCheckInterval = 500;
        private Thread connectionThread = null;

        /// <summary>
        /// Thread process which checks whether the socket is still connected to the network.
        /// </summary>
        private void CheckSocketProc()
        {
            /* TODO
             * Unclear why this is necessary. Sometimes, right after a new TcpClient is created, the socket says
             * it has been shutdown. This catches that case and reconnects.
            */

            while (connected)
            {
                // Reconnect if the socket is disconnected
                if (!CheckSocketConnection(client.Client))
                {
                    Debug.LogWarning("Socket is unexpectedly disconnected. Reconnecting...");
                    taskQueue.Enqueue(() => Connect());
                    break;
                }
                else
                {
                    Thread.Sleep(connectionCheckInterval);
                }
            }
        }

        /// <summary>
        /// Checks whether the socket is still connected to the network.
        /// </summary>
        private bool CheckSocketConnection(Socket socket)
        {
            var poll = socket.Poll(1000, SelectMode.SelectRead);
            var avail = (socket.Available == 0);
            if ((poll && avail) || !socket.Connected)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

}