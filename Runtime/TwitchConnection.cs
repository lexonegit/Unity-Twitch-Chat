using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;

namespace Incredulous.Twitch
{

    internal partial class TwitchConnection
    {
        public TwitchConnection(TwitchIRC twitchIRC)
        {
            try
            {
                tcpClient = new TcpClient(twitchIRC.ircAddress, twitchIRC.port);
            }
            catch (Exception)
            {
                tcpClient = null;
            }

            twitchCredentials = twitchIRC.twitchCredentials;
            clientUserTags = twitchIRC.clientUserTags;

            readBufferSize = twitchIRC.readBufferSize;
            readInterval = twitchIRC.readInterval;
            writeInterval = twitchIRC.writeInterval;

            alertQueue = twitchIRC.alertQueue;
            chatterQueue = twitchIRC.chatterQueue;

            chatRateLimit = twitchCredentials.username == twitchCredentials.channel ? RateLimit.ChatModerator : RateLimit.ChatRegular;
            outputTimestamps = twitchIRC.outputTimestamps;

            debugIRC = twitchIRC.debugIRC;
            debugThreads = twitchIRC.debugThreads;
        }

        /// <summary>
        /// The TCP Client instance for this connection.
        /// </summary>
        public TcpClient tcpClient { get; private set; }

        /// <summary>
        /// The client user's Twitch tags.
        /// </summary>
        public IRCTags clientUserTags
        {
            get => _clientUserTags;
            private set => Interlocked.Exchange(ref _clientUserTags, value);
        }
        private IRCTags _clientUserTags;

        /// <summary>
        /// Whether this instance is currently connected to Twitch.
        /// </summary>
        public bool isConnnected
        {
            get => _isConnected == 1;
            private set => Interlocked.Exchange(ref _isConnected, value ? 1 : 0);
        }
        private int _isConnected;

        /// <summary>
        /// Whether this connection has received a disconnect request.
        /// </summary>
        public bool pendingDisconnect;

        private readonly TwitchCredentials twitchCredentials;
        private readonly int readBufferSize;
        private readonly int readInterval;
        private readonly int writeInterval;
        private readonly bool debugIRC;
        private readonly bool debugThreads;

        /// <summary>
        /// A reference to the TwitchIRC manager's alert queue.
        /// </summary>
        private readonly ConcurrentQueue<ConnectionAlert> alertQueue;

        /// <summary>
        /// A reference to the TwitchIRC manager's chat message queue.
        /// </summary>
        private readonly ConcurrentQueue<Chatter> chatterQueue;

        /// <summary>
        /// A reference to the TwitchIRC manager's output timestamp queue (for rate limiting).
        /// </summary>
        private readonly ConcurrentQueue<DateTime> outputTimestamps;

        private Thread sendThread;
        private Thread receiveThread;

        private bool continueThreads
        {
            get => _continueThreads == 1;
            set => Interlocked.Exchange(ref _continueThreads, value ? 1 : 0);
        }
        private int _continueThreads = 1;

        private RateLimit chatRateLimit;
        private object rateLimitLock = new object();

        /// <summary>
        /// Initalizes a connection to Twitch and starts the send, receive, and check connection threads.
        /// </summary>
        public void Begin()
        {
            receiveThread = new Thread(() => ReceiveProcess());
            sendThread = new Thread(() => SendProcess());

            receiveThread.Start();
            sendThread.Start();

            // Queue login commands
            SendCommand("PASS oauth:" + twitchCredentials.oauth.ToLower(), true);
            SendCommand("NICK " + twitchCredentials.username.ToLower(), true);
            SendCommand("CAP REQ :twitch.tv/tags twitch.tv/commands", true);
        }

        /// <summary>
        /// A coroutine which closes the connection and threads without blocking the main thread.
        /// </summary>
        public IEnumerator End()
        {
            if (tcpClient == null || pendingDisconnect)
                yield break;

            pendingDisconnect = true;

            isConnnected = false;
            continueThreads = false;

            while (receiveThread.IsAlive)
                yield return null;
            while (sendThread.IsAlive)
                yield return null;

            tcpClient.Close();
        }

        /// <summary>
        /// Terminates the connection to Twitch and blocks the main thread while the send, receive, and check connection threads end.
        /// </summary>
        public void BlockingEndAndClose()
        {
            if (tcpClient == null)
                return;

            pendingDisconnect = true;
            isConnnected = false;
            continueThreads = false;
            receiveThread?.Join();
            sendThread?.Join();
            tcpClient.Close();
        }

        /// <summary>
        /// Updates the rate limit based on the tags received from a USERSTATE message.
        /// </summary>
        private void UpdateRateLimits(IRCTags tags)
        {
            if (tags.HasBadge("broadcaster") || tags.HasBadge("moderator"))
            {
                lock (rateLimitLock)
                    chatRateLimit = RateLimit.ChatModerator;
            }
            else
            {
                lock (rateLimitLock)
                    chatRateLimit = RateLimit.ChatRegular;
            }
        }
    }

}