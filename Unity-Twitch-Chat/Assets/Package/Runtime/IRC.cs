using System;
using System.Collections;
using UnityEngine;
using System.Collections.Concurrent;

namespace Lexone.UnityTwitchChat
{
    [AddComponentMenu("Unity Twitch Chat/Twitch IRC")]
    public partial class IRC : MonoBehaviour
    {
        [Header("Twitch IRC address and port")]

        [SerializeField] public string address = "irc.chat.twitch.tv";
        [SerializeField] public int port = 6667;

        [Header("Twitch IRC connection")]

        [Tooltip("If true, the client will connect to Twitch IRC anonymously (OAuth and username will be ignored)\n\nNote that you can't send chat messages when using anonymous login.")]
        [SerializeField] private bool useAnonymousLogin = false;

        [Tooltip("The OAuth token which will be used to authenticate with Twitch.\n\nGenerate one at: https://twitchapps.com/tmi/")]
        [SerializeField] public string oauth = "";

        [Tooltip("The Twitch username which will be used to authenticate with Twitch IRC.\n\n(this is the login name, not display name)")]
        [SerializeField] public string username = "";

        [Tooltip("The Twitch channel name which the client will join.")]
        [SerializeField] public string channel = "";

        [Header("General settings")]

        [Tooltip("If true, duplicate instances will be destroyed. The first instance will be set to DontDestroyOnLoad.")]
        [SerializeField] public bool singleton = true;

        [Tooltip("If true, the client will connect to Twitch IRC on Start.")]
        [SerializeField] private bool connectOnStart = true;

        [Tooltip("If true, every IRC message sent and received will be logged to the console.")]
        [SerializeField] public bool showIRCDebug = true;

        [Tooltip("If true, the thread start and stop will be logged to the console.")]
        [SerializeField] public bool showThreadDebug = true;

        [Tooltip("If true, chatters who haven't set their name color on Twitch will be assigned a random color, instead of white.")]
        [SerializeField] public bool useRandomColorForUndefined = false;

        [Header("Chat read settings (read thread)")]

        [Tooltip("The number of milliseconds between each time the read thread checks for new messages.")]
        [SerializeField] public int readInterval = 50;
        [Tooltip("The capacity of the read buffer. Smaller values consume less memory but require more cycles to retrieve data (CPU usage)")]
        [SerializeField] public ReadBufferSize readBufferSize = ReadBufferSize._256;

        [Header("Chat write settings (write thread)")]

        [Tooltip("The number of milliseconds between each time the write thread checks its queues.")]
        public int writeInterval = 50;


        // If the game is paused for a significant amount of time and then unpaused,
        // there could be a lot of data to handle, which could cause lag spikes.
        // To prevent this, we limit the amount of data handled per frame.
        private static readonly int maxDataPerFrame = 100;
        private int connectionFailCount = 0;
        private TwitchConnection connection;
        public static IRC Instance { get; private set; }

        #region Events

        public event Action<Chatter> OnChatMessage;
        public event Action<IRCReply> OnConnectionAlert;

        #endregion

        // Queues
        internal readonly ConcurrentQueue<IRCReply> alertQueue = new ConcurrentQueue<IRCReply>();
        internal readonly ConcurrentQueue<Chatter> chatterQueue = new ConcurrentQueue<Chatter>();

        [ContextMenu("Ping")]
        public void Ping() => connection?.Ping();
        public IRCTags ClientUserTags => connection?.ClientUserTags;

        #region Unity methods

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

        private void OnDestroy()
        {
            if (singleton && Instance == this)
                Instance = null;

            BlockingDisconnect();
        }

        private void OnDisable()
        {
            BlockingDisconnect();
        }

        #endregion

        private void HandlePendingInformation()
        {
            int dataHandledThisFrame = 0;

            // Handle pending connection alerts
            while (!alertQueue.IsEmpty)
            {
                if (dataHandledThisFrame >= maxDataPerFrame)
                    break;

                if (alertQueue.TryDequeue(out var alert))
                {
                    HandleConnectionAlert(alert);
                    dataHandledThisFrame++;
                }
            }

            // Handle pending chat messages
            while (!chatterQueue.IsEmpty)
            {
                if (dataHandledThisFrame >= maxDataPerFrame)
                    break;

                if (chatterQueue.TryDequeue(out var chatter))
                {
                    OnChatMessage?.Invoke(chatter);
                    dataHandledThisFrame++;
                }
            }
        }

        private void HandleConnectionAlert(IRCReply alert)
        {
            if (showIRCDebug)
                Debug.Log($"{Tags.alert} {alert.GetDescription()}");

            switch (alert)
            {
                case IRCReply.NO_CONNECTION:
                case IRCReply.BAD_LOGIN:
                case IRCReply.MISSING_LOGIN_INFO:
                    connectionFailCount = 0;
                    Disconnect();
                    break;

                case IRCReply.CONNECTED_TO_SERVER:
                    break;

                case IRCReply.CONNECTION_INTERRUPTED:
                    // Increment fail count and try reconnecting
                    connectionFailCount++;
                    Connect();
                    break;

                case IRCReply.JOINED_CHANNEL:
                    connectionFailCount = 0;
                    break;
            }

            OnConnectionAlert?.Invoke(alert);
        }

        [ContextMenu("Connect")]
        public void Connect()
        {
            if (useAnonymousLogin)
            {
                // Anonymous login
                username = "justinfan" + UnityEngine.Random.Range(1000, 9999);
                oauth = "";
            }
            else
            {
                // Real user login
                if (oauth.Length <= 0 || username.Length <= 0)
                {
                    alertQueue.Enqueue(IRCReply.MISSING_LOGIN_INFO);
                    return;
                }

                // Fix formatting (twitchapps.com)
                if (oauth.StartsWith("oauth:"))
                    oauth = oauth.Substring(6);
            }

            if (channel.Length <= 0)
            {
                alertQueue.Enqueue(IRCReply.MISSING_LOGIN_INFO);
                return;
            }

            StartCoroutine(StartConnection());
            IEnumerator StartConnection()
            {
                if (connection != null) // End current connection if it exists
                    yield return StartCoroutine(NonBlockingDisconnect());

                connection = new TwitchConnection(this);

                if (connection.tcpClient == null || !connection.tcpClient.Connected)
                {
                    alertQueue.Enqueue(IRCReply.NO_CONNECTION);
                    yield break;
                }

                // Reconnect interval based on failed attempt count
                if (connectionFailCount >= 2)
                {
                    int delay = 1 << (connectionFailCount - 2); // -> 0s, 1s, 2s, 4s, 8s, 16s, ...

                    if (showIRCDebug)
                        Debug.Log($"{Tags.alert} Reconnecting in {delay} seconds");

                    yield return new WaitForSecondsRealtime(delay);
                }

                // Start connection and threads
                connection.Begin();
            }
        }

        [ContextMenu("Disconnect")]
        public void Disconnect()
        {
            if (connection == null || connection.disconnectCalled)
                return;

            StartCoroutine(NonBlockingDisconnect());
        }

        private IEnumerator NonBlockingDisconnect()
        {
            yield return StartCoroutine(connection.End());

            // Reset connection variable
            connection = null;

            if (showIRCDebug)
                Debug.Log($"{Tags.alert} Disconnected from Twitch IRC");
        }

        private void BlockingDisconnect()
        {
            if (connection == null)
                return;

            connection.BlockingEnd();

            // Reset connection variable
            connection = null;

            if (showIRCDebug)
                Debug.Log($"{Tags.alert} Disconnected from Twitch IRC");
        }

        /// <summary>
        /// Sends a chat message to the channel
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendChatMessage(string message)
        {
            if (useAnonymousLogin)
            {
                Debug.LogWarning("Chat messages cannot be sent with anonymous login");
                return;
            }

            connection.SendChatMessage(message);
        }

        /// <summary>
        /// Join a new channel
        /// </summary>
        /// <param name="channel">The channel to join</param>
        public void JoinChannel(string channel)
        {
            if (channel != "")
                connection.SendCommand("JOIN #" + channel.ToLower(), true);
        }

        /// <summary>
        /// Leaves a channel
        /// </summary>
        /// <param name="channel">The channel to leave</param>
        public void LeaveChannel(string channel)
        {
            if (channel != "")
                connection.SendCommand("PART #" + channel.ToLower(), true);
        }
    }
}