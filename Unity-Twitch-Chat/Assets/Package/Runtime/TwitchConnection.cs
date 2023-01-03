using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

namespace UnityTwitchChat
{
    internal partial class TwitchConnection
    {
        public TwitchConnection(IRC irc)
        {
            try
            {
                tcpClient = new TcpClient(irc.address, irc.port);
            }
            catch
            {
                tcpClient = null;
            }

            this.oauth = irc.oauth;
            this.nick = irc.username;
            this.channel = irc.channel;

            this.readBufferSize = irc.readBufferSize;
            this.readInterval = irc.readInterval;
            this.writeInterval = irc.writeInterval;

            this.alertQueue = irc.alertQueue;
            this.chatterQueue = irc.chatterQueue;

            this.rateLimit = RateLimit.ChatRegular;

            this.showIRCDebug = irc.showIRCDebug;
            this.showThreadDebug = irc.showThreadDebug;
        }

        public TcpClient tcpClient { get; private set; }

        private IRCTags _clientUserTags;
        private IRCTags ClientUserTags
        {
            get => _clientUserTags;
            set => Interlocked.Exchange(ref _clientUserTags, value);
        }

        private int _threadsRunning = 1;
        private bool ThreadsRunning
        {
            get => _threadsRunning == 1;
            set => Interlocked.Exchange(ref _threadsRunning, value ? 1 : 0);
        }

        public bool disconnectCalled = false;

        private readonly string oauth;
        private readonly string nick;
        private readonly string channel;
        private readonly int readBufferSize;
        private readonly int readInterval;
        private readonly int writeInterval;
        private readonly bool showIRCDebug;
        private readonly bool showThreadDebug;

        private readonly ConcurrentQueue<IRCReply> alertQueue;
        private readonly ConcurrentQueue<Chatter> chatterQueue;

        private Thread readThread;
        private Thread writeThread;

        private RateLimit rateLimit = RateLimit.ChatRegular;
        private object rateLimitLock = new object();

        public void Begin()
        {
            readThread = new Thread(() => ReadThreadLoop());
            writeThread = new Thread(() => WriteThreadLoop());

            readThread.Start();
            writeThread.Start();

            // Send connection commands
            SendCommand("PASS oauth:" + oauth.ToLower(), true);
            SendCommand("NICK " + nick.ToLower(), true);
            SendCommand("CAP REQ :twitch.tv/tags twitch.tv/commands", true); // twitch.tv/membership
        }

        public IEnumerator End()
        {
            if (tcpClient == null || disconnectCalled)
                yield break;

            disconnectCalled = true;
            ThreadsRunning = false;

            // Wait for the threads to stop
            while (readThread.IsAlive)
                yield return null;
            while (writeThread.IsAlive)
                yield return null;

            tcpClient.Close();
        }

        public void BlockingEnd()
        {
            if (tcpClient == null)
                return;

            disconnectCalled = true;
            ThreadsRunning = false;
            readThread?.Join();
            writeThread?.Join();
            tcpClient.Close();
        }

        private void UpdateRateLimits()
        {
            if (ClientUserTags.HasBadge("broadcaster") || ClientUserTags.HasBadge("moderator"))
            {
                lock (rateLimitLock)
                    rateLimit = RateLimit.ChatModerator;
            }
            else
            {
                lock (rateLimitLock)
                    rateLimit = RateLimit.ChatRegular;
            }
        }

    }
}