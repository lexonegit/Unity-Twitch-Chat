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

            readBufferSize = twitchIRC.readBufferSize;
            readInterval = twitchIRC.readInterval;
            writeInterval = twitchIRC.writeInterval;

            debugIRC = twitchIRC.debugIRC;
        }

        public IRCUserstate userstate { get; private set; }
        public IRCTags userTags { get; private set; }
        public ConcurrentQueue<Chatter> chatterQueue { get; private set;} = new ConcurrentQueue<Chatter>();
        public ConcurrentQueue<ConnectionAlert> connectionAlertQueue { get; private set; } = new ConcurrentQueue<ConnectionAlert>();
        public TcpClient tcpClient { get; private set; }
        public ConnectionStatus status { get; private set; }
        public bool threadsActive => (receiveThread?.IsAlive ?? false) || (sendThread?.IsAlive ?? false) || (connectionThread?.IsAlive ?? false);

        readonly TwitchCredentials twitchCredentials;
        readonly int readBufferSize;
        readonly int readInterval;
        readonly int writeInterval;
        readonly bool debugIRC;

        Thread sendThread;
        Thread receiveThread;
        Thread connectionThread;

        bool continueThreads
        {
            get => _continueThreads == 1;
            set => Interlocked.Exchange(ref _continueThreads, value ? 1 : 0);
        }
        int _continueThreads = 1;

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

        public void End()
        {
            status = ConnectionStatus.DisconnectionPending;
            continueThreads = false;
        }

        public void Close()
        {
            tcpClient.Close();
            status = ConnectionStatus.Disconnected;
        }

        public void BlockingEndAndClose()
        {
            status = ConnectionStatus.DisconnectionPending;
            continueThreads = false;
            receiveThread?.Join();
            sendThread?.Join();
            connectionThread?.Join();
            tcpClient.Close();
            status = ConnectionStatus.Disconnected;
        }
    }

}