using System.Collections;
using System.Collections.Generic;
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
        public bool debugIRC = true;


        /// <summary>
        /// The client user's Twitch tags.
        /// </summary>
        public IRCTags clientUserTags { get; private set; }

        /// <summary>
        /// The current connection status.
        /// </summary>
        public ConnectionStatus status => connection?.status ?? ConnectionStatus.Disconnected;


        /// <summary>
        /// The current Twitch connection.
        /// </summary>
        private TwitchConnection connection;

        /// <summary>
        /// A queue for connection alerts.
        /// </summary>
        private Queue<ConnectionAlert> alertQueue = new Queue<ConnectionAlert>();

        #region Unity MonoBehaviour Messages

        private void Start()
        {
            if (connectOnStart)
                StartCoroutine(ConnectCoroutine());
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

            while (!connection.connectionAlertQueue.IsEmpty)
            {
                if (connection.connectionAlertQueue.TryDequeue(out var alert))
                    HandleConnectionAlert(alert);
            }

            while (alertQueue.Count > 0)
            {
                var alert = alertQueue.Dequeue();
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
        /// A coroutine which builds the connection to the Twitch IRC server and starts the send/receive threads.
        /// </summary>
        private IEnumerator ConnectCoroutine()
        {
            // End any current connection
            StartCoroutine(DisconnectCoroutine(connection));

            // Verify that login information has been provided
            if (twitchCredentials.oauth.Length <= 0 || twitchCredentials.username.Length <= 0 || twitchCredentials.channel.Length <= 0)
            {
                ConnectionAlertEvent?.Invoke(ConnectionAlert.MissingLogin);
                yield break;
            }

            // Fix formatting (twitchapps.com)
            if (twitchCredentials.oauth.StartsWith("oauth:"))
                twitchCredentials.oauth = twitchCredentials.oauth.Substring(6);

            // Create a new connection to Twitch IRC
            connection = new TwitchConnection(this);

            // Check the connection
            if (!connection.tcpClient.Connected)
            {
                ConnectionAlertEvent?.Invoke(ConnectionAlert.NoConnection);
                yield break;
            }

            // Begin the threads and attempt to authenticate
            connection.Begin();
        }

        /// <summary>
        /// A coroutine which instructs the send/receive threads to terminate, waits for them to end, then closes the TCP connection.
        /// </summary>
        private IEnumerator DisconnectCoroutine(TwitchConnection connection)
        {
            if (connection == null)
                yield break;

            if (connection.status == ConnectionStatus.Disconnected || connection.status == ConnectionStatus.DisconnectionPending)
                yield break;

            // Stop the connection
            connection.End();

            // Wait for threads to close
            while (connection.threadsActive)
                yield return null;

            // Close the TcpClient
            connection.Close();

            // Reset connection
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

            if (connection.status == ConnectionStatus.Disconnected || connection.status == ConnectionStatus.DisconnectionPending)
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
            ConnectionAlertEvent?.Invoke(alert);
            if (alert.isError)
            {
                Debug.LogError(alert.message);
            }
            else
            {
                Debug.Log(alert.message);
            }

            // Reconnect if connection is interrupted
            if (alert.status == ConnectionAlert.CONNECTION_INTERRUPTED)
                Connect();
        }
    }

}