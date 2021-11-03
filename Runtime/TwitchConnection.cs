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
            tcpClient = new TcpClient(twitchIRC.ircAddress, twitchIRC.port);
            twitchCredentials = twitchIRC.twitchCredentials;
            clientUserTags = twitchIRC.clientUserTags;

            readBufferSize = twitchIRC.readBufferSize;
            readInterval = twitchIRC.readInterval;
            writeInterval = twitchIRC.writeInterval;

            alertQueue = twitchIRC.alertQueue;

            debugIRC = twitchIRC.debugIRC;
            debugThreads = twitchIRC.debugThreads;
        }

        /// <summary>
        /// The TCP Client instance for this connection.
        /// </summary>
        public TcpClient tcpClient { get; private set; }

        /// <summary>
        /// The queue of received messages from Twitch.
        /// </summary>
        public ConcurrentQueue<Chatter> chatterQueue { get; private set; } = new ConcurrentQueue<Chatter>();

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

        private readonly TwitchCredentials twitchCredentials;
        private readonly int readBufferSize;
        private readonly int readInterval;
        private readonly int writeInterval;
        private readonly bool debugIRC;
        private readonly bool debugThreads;

        /// <summary>
        /// A reference to the TwitchIRC manager's alert queue.
        /// </summary>
        private ConcurrentQueue<ConnectionAlert> alertQueue;

        /// <summary>
        /// A reference to the TwitchIRC manager's task queue.
        /// </summary>
        private ConcurrentQueue<System.Action> taskQueue;

        private Thread sendThread;
        private Thread receiveThread;
        private Thread connectionThread;

        private bool pendingDisconnect;

        private bool continueThreads
        {
            get => _continueThreads == 1;
            set => Interlocked.Exchange(ref _continueThreads, value ? 1 : 0);
        }
        private int _continueThreads = 1;

        /// <summary>
        /// Initalizes a connection to Twitch and starts the send, receive, and check connection threads.
        /// </summary>
        public void Begin()
        {
            receiveThread = new Thread(() => ReceiveProcess());
            sendThread = new Thread(() => SendProcess());
            connectionThread = new Thread(() => CheckConnectionProcess());

            receiveThread.Start();
            sendThread.Start();
            connectionThread.Start();

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
            if (pendingDisconnect)
                yield break;

            pendingDisconnect = true;

            isConnnected = false;
            continueThreads = false;

            while (receiveThread.IsAlive)
                yield return null;
            while (sendThread.IsAlive)
                yield return null;
            while (connectionThread.IsAlive)
                yield return null;

            tcpClient.Close();
        }

        /// <summary>
        /// Terminates the connection to Twitch and blocks the main thread while the send, receive, and check connection threads end.
        /// </summary>
        public void BlockingEndAndClose()
        {
            if (pendingDisconnect)
                return;

            pendingDisconnect = true;

            isConnnected = false;
            continueThreads = false;
            receiveThread?.Join();
            sendThread?.Join();
            connectionThread?.Join();
            tcpClient.Close();
        }
    }

}