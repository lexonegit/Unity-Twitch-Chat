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
        public TwitchCredentials twitchCredentials;


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
        /// Whether a connection to Twitch should be established on Start.
        /// </summary>
        public bool connectOnStart = true;

        /// <summary>
        /// Whether all IRC messages should be logged to the debug console.
        /// </summary>
        public bool debugIRC = false;


        /// <summary>
        /// The client user's Twitch tags.
        /// </summary>
        public IRCTags clientUserTags { get; private set; }

        /// <summary>
        /// Whether the Twitch client is successfully connected to Twitch.
        /// </summary>
        public bool IsConnected => connection?.isConnnected ?? false;


        /// <summary>
        /// A queue for connection alerts.
        /// </summary>
        internal readonly ConcurrentQueue<ConnectionAlert> alertQueue = new ConcurrentQueue<ConnectionAlert>();


        /// <summary>
        /// The current Twitch connection.
        /// </summary>
        private TwitchConnection connection;


        #region Unity MonoBehaviour Messages

        private void Start()
        {
            if (connectOnStart)
                Connect();
        }

        private void Update()
        {
            if (connection == null)
                return;

            while (!connection.chatterQueue.IsEmpty)
            {
                if (connection.chatterQueue.TryDequeue(out var chatter))
                    ChatMessageEvent?.Invoke(chatter);
            }

            while (!alertQueue.IsEmpty)
            {
                if (alertQueue.TryDequeue(out var alert))
                    HandleConnectionAlert(alert);
            }

            if (clientUserTags != connection.clientUserTags)
                clientUserTags = connection.clientUserTags;
        }

        private void OnDestroy()
        {
            BlockingDisconnect(connection);
        }

        private void OnDisable()
        {
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
            // End any current connection
            if (connection != null)
                Disconnect();

            // Verify that login information has been provided
            if (twitchCredentials.oauth.Length <= 0 || twitchCredentials.username.Length <= 0 || twitchCredentials.channel.Length <= 0)
            {
                alertQueue.Enqueue(ConnectionAlert.MissingLogin);
                return;
            }

            // Fix formatting (twitchapps.com)
            if (twitchCredentials.oauth.StartsWith("oauth:"))
                twitchCredentials.oauth = twitchCredentials.oauth.Substring(6);

            // Create a new connection to Twitch IRC
            connection = new TwitchConnection(this);

            // Check the connection
            if (!connection.tcpClient.Connected)
            {
                alertQueue.Enqueue(ConnectionAlert.NoConnection);
                return;
            }

            // Begin the threads and attempt to authenticate
            connection.Begin();
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
        /// A coroutine which instructs the send/receive threads to terminate, waits for them to end, then closes the TCP connection.
        /// </summary>
        private IEnumerator DisconnectCoroutine(TwitchConnection connection)
        {
            if (connection == null)
                yield break;

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

            // End the connection
            connection.BlockingEndAndClose();

            // Reset connection
            if (connection == this.connection)
                this.connection = null;

            Debug.LogWarning("Disconnected from Twitch IRC");
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
                    Disconnect();
                    break;

                case ConnectionAlert.CONNECTION_INTERRUPTED:
                    Debug.LogError(alert.message);
                    Connect();
                    break;

                case ConnectionAlert.CONNECTED_TO_SERVER:
                case ConnectionAlert.JOINED_CHANNEL:
                    Debug.Log(alert.message);
                    break;
            }
            ConnectionAlertEvent?.Invoke(alert);
        }
    }

}