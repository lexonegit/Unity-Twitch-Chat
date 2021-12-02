using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace Incredulous.Twitch
{
    /// <summary>
    /// A class which manages connections to the Twitch IRC API.
    /// </summary>
    public partial class TwitchIRC : MonoBehaviour
    {
        [Header("Server Information")]

        [Tooltip("The IRC Address of the server: irc.chat.twitch.tv")]
        public string ircAddress = "irc.chat.twitch.tv";

        [Tooltip("The server port which the client should connect to: 6667")]
        public int port = 6667;

        [Tooltip("The information used to authenticate with Twitch.")]
        public TwitchCredentials twitchCredentials;

        [Tooltip("Whether the client should use verified-status rate limits. NOTE: Do not set this to true if you have not been granted verified status by Twitch. Your account risks being locked or banned.")]
        public bool verifiedBotStatus;

        [Header("Read Information")]

        [Tooltip("The number of milliseconds between each time the input thread checks for new inputs.")]
        public int readInterval = 100;

        [Tooltip("The capacity of the read buffer. Smaller values consume less memory but require more cycles to retrieve data.")]
        public int readBufferSize = 128;


        [Header("Write Information")]

        [Tooltip("The number of milliseconds between each time the output thread checks its queues.")]
        public int writeInterval = 100;


        [Header("Other Settings")]

        [Tooltip("When true, duplicate instances of TwitchIRC will be destroyed. The first instance will be set to DontDestroyOnLoad.")]
        [SerializeField] private bool singleton;

        [Tooltip("Whether a connection to Twitch should be established on Start.")]
        public bool connectOnStart = true;

        [Tooltip("Whether all IRC messages should be logged to the debug console.")]
        public bool debugIRC = false;

        [Tooltip("Whether thread warning messages should be logged to the debug console.")]
        public bool debugThreads = false;

        /// <summary>
        /// The client user's Twitch tags.
        /// </summary>
        public IRCTags clientUserTags { get; private set; }

        /// <summary>
        /// Whether the Twitch client is successfully connected to Twitch.
        /// </summary>
        public bool IsConnected => connection?.isConnected ?? false;

        /// <summary>
        /// The first created instance of TwitchIRC, if it exists.
        /// </summary>
        public static TwitchIRC Instance { get; private set; }

        /// <summary>
        /// A queue for connection alerts.
        /// </summary>
        internal readonly ConcurrentQueue<ConnectionAlert> alertQueue = new ConcurrentQueue<ConnectionAlert>();

        /// <summary>
        /// A queue for incoming chat messages.
        /// </summary>
        internal readonly ConcurrentQueue<Chatter> chatterQueue = new ConcurrentQueue<Chatter>();

        /// <summary>
        /// A queue which holds timestamps for all outputs sent to the server (for rate limiting).
        /// </summary>
        internal readonly ConcurrentQueue<DateTime> outputTimestamps = new ConcurrentQueue<DateTime>();


        /// <summary>
        /// The current Twitch connection.
        /// </summary>
        private TwitchConnection connection;

        /// <summary>
        /// The number of times a connection attempt has sequentially failed.
        /// </summary>
        private int failCount;


        #region Unity MonoBehaviour Messages

        private void Awake()
        {
            if (Instance)
            {
                if (singleton)
                {
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                }
            }
            else
            {
                Instance = this;

                if (singleton)
                    DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (connectOnStart)
                Connect();
        }

        private void Update()
        {
            HandlePendingInformation();
        }

        private void OnDisable()
        {
            BlockingDisconnect(connection);
        }

        private void OnDestroy()
        {
            if (singleton && Instance == this) Instance = null;
            BlockingDisconnect(connection);
        }

        #endregion

        #region Events

        /// <summary>
        /// A delegate which handles a new chat message from the server.
        /// </summary>
        public delegate void ChatMessageEventHandler(Chatter chatter);

        /// <summary>
        /// A delegate which handles a status update from the Twitch IRC client.
        /// </summary>
        public delegate void ConnectionAlertEventHandler(ConnectionAlert connectionAlert);

        /// <summary>
        /// An event which is triggered when a new chat message is received.
        /// </summary>
        public event ChatMessageEventHandler ChatMessageEvent;

        /// <summary>
        /// An event which is triggered when the connection status changes.
        /// </summary>
        public event ConnectionAlertEventHandler ConnectionAlertEvent;

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
            StartCoroutine(DisconnectCoroutine(connection));
        }

        /// <summary>
        /// Sends a chat message.
        /// </summary>
        /// <param name="message"></param>
        public void SendChatMessage(string message) => connection?.SendChatMessage(message);

        /// <summary>
        /// Queues a command to be sent to the IRC server. Prioritzed commands will be sent without regard for rate limits.
        /// </summary>
        public void SendCommand(string command, bool prioritized = false) => connection?.SendCommand(command, prioritized);

        /// <summary>
        /// Sends a PING message to the Twitch IRC server.
        /// </summary>
        [ContextMenu("Ping Server")]
        public void Ping() => connection?.Ping();

        private IEnumerator ConnectCoroutine()
        {
            // End any current connection
            if (connection != null)
                Disconnect();

            // Verify that login information has been provided
            if (twitchCredentials.oauth.Length <= 0 || twitchCredentials.username.Length <= 0 || twitchCredentials.channel.Length <= 0)
            {
                alertQueue.Enqueue(ConnectionAlert.MissingLogin);
                yield break;
            }

            // Fix formatting (twitchapps.com)
            if (twitchCredentials.oauth.StartsWith("oauth:"))
                twitchCredentials.oauth = twitchCredentials.oauth.Substring(6);

            // Create a new connection to Twitch IRC
            connection = new TwitchConnection(this);

            // Check the connection
            if (connection.tcpClient == null || !connection.tcpClient.Connected)
            {
                alertQueue.Enqueue(ConnectionAlert.NoConnection);
                yield break;
            }

            // Wait for an interval if there has been more than one failed connection attempt
            if (failCount >= 2)
                yield return new WaitForSecondsRealtime(1 << (failCount - 2));

            // Begin the threads and attempt to authenticate
            connection.Begin();
        }

        /// <summary>
        /// A coroutine which instructs the send/receive threads to terminate, waits for them to end, then closes the TCP connection.
        /// </summary>
        private IEnumerator DisconnectCoroutine(TwitchConnection connection)
        {
            if (connection == null || connection.pendingDisconnect)
                yield break;

            // Finish processing any pending information
            HandlePendingInformation();

            // Close the connection
            yield return StartCoroutine(connection.End());

            // Reset connection variable
            if (this.connection == connection)
                this.connection = null;

            Debug.LogWarning("Disconnected from Twitch IRC");
        }

        /// <summary>
        /// Disconnect from Twitch IRC. <b>Blocks the main thread. Use carefully.</b>
        /// </summary>
        private void BlockingDisconnect(TwitchConnection connection)
        {
            if (connection == null)
                return;

            // Finish processing any pending information
            HandlePendingInformation();

            // End the connection
            connection.BlockingEndAndClose();

            // Reset connection
            if (connection == this.connection)
                this.connection = null;

            Debug.LogWarning("Disconnected from Twitch IRC");
        }

        /// <summary>
        /// Handles pending information received from the current connection
        /// </summary>
        private void HandlePendingInformation()
        {
            while (!chatterQueue.IsEmpty)
            {
                if (chatterQueue.TryDequeue(out var chatter))
                    ChatMessageEvent?.Invoke(chatter);
            }

            while (!alertQueue.IsEmpty)
            {
                if (alertQueue.TryDequeue(out var alert))
                    HandleConnectionAlert(alert);
            }

            if (connection != null && clientUserTags != connection.clientUserTags)
                clientUserTags = connection.clientUserTags;
        }

        /// <summary>
        /// Handles a connection alert and propogates it to any listeners.
        /// </summary>
        private void HandleConnectionAlert(ConnectionAlert alert)
        {
            switch (alert.status)
            {
                case ConnectionAlert.BAD_LOGIN:
                case ConnectionAlert.MISSING_LOGIN:
                case ConnectionAlert.NO_CONNECTION:
                    Debug.LogError(alert.message);
                    failCount = 0;
                    Disconnect();
                    break;

                case ConnectionAlert.CONNECTION_INTERRUPTED:
                    Debug.LogError(alert.message);
                    failCount++;
                    Connect();
                    break;

                case ConnectionAlert.JOINED_CHANNEL:
                    Debug.Log(alert.message);
                    failCount = 0;
                    break;

                default:
                    Debug.Log(alert.message);
                    break;
            }
            ConnectionAlertEvent?.Invoke(alert);
        }
    }

}