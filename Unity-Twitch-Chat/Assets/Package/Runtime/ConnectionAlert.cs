namespace Incredulous.Twitch
{

    public class ConnectionAlert
    {
        public ConnectionAlert(int status, string message)
        {
            this.status = status;
            this.message = message;
        }

        public readonly int status;
        public readonly string message;

        public bool isError => status >= 400 && status <= 599;

        public static readonly ConnectionAlert RateLimitWarning = new ConnectionAlert(RATE_LIMIT_WARNING, $"{tagWarning} Too many commands/messages have been sent. Pending messages will not be sent until cooldown has ended.");
        public static readonly ConnectionAlert ConnectedToServer = new ConnectionAlert(CONNECTED_TO_SERVER, $"{tagConnection} Connected to server.");
        public static readonly ConnectionAlert Pong = new ConnectionAlert(PONG, $"{tagStatus} Server responded with PONG");
        public static readonly ConnectionAlert JoinedChannel = new ConnectionAlert(JOINED_CHANNEL, $"{tagJoin} Successfully joined channel!");
        public static readonly ConnectionAlert MissingLogin = new ConnectionAlert(MISSING_LOGIN, $"{tagError} Missing login information. Please check your credentials.");
        public static readonly ConnectionAlert BadLogin = new ConnectionAlert(BAD_LOGIN, $"{tagError} Login authentication failed.");
        public static readonly ConnectionAlert ConnectionInterrupted = new ConnectionAlert(CONNECTION_INTERRUPTED, $"{tagError} Connection was unexpectedly ended.");
        public static readonly ConnectionAlert NoConnection = new ConnectionAlert(NO_CONNECTION, $"{tagError} Could not reach server.");

        public const int RATE_LIMIT_WARNING = -1;
        public const int CONNECTED_TO_SERVER = 001;
        public const int PONG = 007;
        public const int JOINED_CHANNEL = 353;
        public const int MISSING_LOGIN = 431;
        public const int BAD_LOGIN = 464;
        public const int CONNECTION_INTERRUPTED = 498;
        public const int NO_CONNECTION = 499;

        public const string tagError = "<color=red><b>[ERROR]</b></color>";
        public const string tagStatus = "<color=#0018a1><b>[STATUS]</b></color>";
        public const string tagConnection = "<color=#0ea300><b>[CONNECT]</b></color>";
        public const string tagJoin = "<color=#bd2881><b>[JOIN]</b></color>";
        public const string tagWarning = "<color=yellow><b>[WARNING]</b></color>";
    }

}