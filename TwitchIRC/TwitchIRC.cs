using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using UnityEngine;

public static class NetworkStreamExtensionMethods
{
    public static void WriteLine(this NetworkStream stream, string output, bool debug = false)
    {
        if (debug)
            Debug.Log("<color=#c91b00><b>[IRC OUTPUT]</b></color> Sending command: " + output);

        byte[] bytes = Encoding.UTF8.GetBytes(output);
        stream.Write(bytes, 0, bytes.Length);
        stream.WriteByte((byte)'\r');
        stream.WriteByte((byte)'\n');
        stream.Flush();
    }
}

public class TwitchIRC : MonoBehaviour
{
    [HideInInspector] public class NewChatMessageEvent : UnityEngine.Events.UnityEvent<Chatter> { }
    [HideInInspector] public class StatusEvent : UnityEngine.Events.UnityEvent<StatusType, string, int> { }

    // Events
    public NewChatMessageEvent newChatMessageEvent = new NewChatMessageEvent();
    public StatusEvent statusEvent = new StatusEvent();

    private TcpClient client;
    private NetworkStream stream;

    public Settings settings;
    public UserInput details;

    [Header("Client chatter object")]
    public Chatter userChatter;

    private bool connected = false;

    [System.Serializable]
    public class UserInput
    {
        public string oauth = string.Empty;
        public string nick = string.Empty;
        public string channel = string.Empty;
    }

    [System.Serializable]
    public class Settings
    {
        public string server = "irc.chat.twitch.tv";
        public int port = 6667;
        [Space(12f)]
        public bool connectOnStart = true;
        public bool parseBadges = true;
        public bool parseTwitchEmotes = true;
        [Space(12f)]
        public bool debugIRC = true;
    }

    private void Awake()
    {
        if (settings.connectOnStart)
            Connect();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnDisable()
    {
        Disconnect();
    }

    [ContextMenu("Connect")]
    public void Connect()
    {
        if (details.oauth.Length <= 0 || details.nick.Length <= 0 || details.channel.Length <= 0)
        {
            ConnectionStateAlert(StatusType.Error, "Missing required details!");
            return;
        }

        //Fix formatting (twitchapps.com)
        if (details.oauth.StartsWith("oauth:"))
            details.oauth = details.oauth.Substring(6);

        ConnectIRC();
    }

    public enum StatusType { Normal, Success, Error };
    public void ConnectionStateAlert(StatusType state, string message, int percentage = 0)
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
        statusEvent.Invoke(state, message, percentage);
    }

    [ContextMenu("Connect IRC")]
    public void ConnectIRC()
    {
        client = new TcpClient(settings.server, settings.port); // Connect to Twitch IRC
        stream = client.GetStream();

        stream.WriteLine("PASS oauth:" + details.oauth.ToLower());
        stream.WriteLine("NICK " + details.nick.ToLower());
        stream.WriteLine("CAP REQ :twitch.tv/tags twitch.tv/commands");

        connected = true;

        inputThread = new Thread(() => IRCInputProc());
        outputThread = new Thread(() => IRCOutputProc());

        // Start threads
        inputThread.Start();
        outputThread.Start();
    }


    [ContextMenu("Disconnect")]
    public void Disconnect()
    {
        if (!connected) return;

        connected = false; // Stop threads

        client.Close();
        stream.Close();

        Debug.LogWarning("Disconnected from Twitch IRC");
    }


    private Thread outputThread;
    private Thread inputThread;
    private void IRCInputProc()
    {
        Debug.Log("IRCInput Thread (Receive) started");

        StreamReader reader = new StreamReader(stream);
        string raw;

        while (connected)
        {
            // try-catch is needed because ReadLine() is a blocking call and disconnecting will cause an exception
            try { raw = reader.ReadLine(); }
            catch { break; }

            if (raw == null) // Ignore empty lines
                continue;

            if (settings.debugIRC)
                Debug.Log("<color=#005ae0><b>[IRC INPUT]</b></color> " + raw);

            string ircString = raw;
            string tagString = string.Empty;

            if (raw[0] == '@')
            {
                int ind = raw.IndexOf(' ');

                tagString = raw.Substring(0, ind);
                ircString = raw.Substring(ind).TrimStart();
            }

            if (ircString[0] == ':')
            {
                string type = ircString.Substring(ircString.IndexOf(' ')).TrimStart();
                type = type.Substring(0, type.IndexOf(' '));

                switch (type)
                {
                    case "PRIVMSG": //Message
                        HandlePRIVMSG(ircString, tagString);
                        break;
                    case "USERSTATE": //Userstate
                        HandleUSERSTATE(ircString, tagString);
                        break;
                    case "353": //Successful channel join
                        HandleRPL(type);
                        break;
                    case "001": //Successful IRC connection
                        HandleRPL(type);
                        break;
                }
            }

            //Respond to PING messages
            if (raw.StartsWith("PING"))
            {
                SendCommand("PONG :tmi.twitch.tv", true);
            }
        }

        Debug.LogWarning("IRCInput Thread (Receive) exited");
    }

    private Queue<string> outputQueue = new Queue<string>();
    private void IRCOutputProc()
    {
        Debug.Log("IRCOutput Thread (Send) started");

        System.Diagnostics.Stopwatch cooldown = new System.Diagnostics.Stopwatch();
        
        //Read loop
        while (connected)
        {
            if (outputQueue.Count <= 0)
                continue;

            string output = outputQueue.Dequeue();

            // Send the output
            stream.WriteLine(output, settings.debugIRC);

            //Cooldown timer
            cooldown.Restart();
            while (cooldown.ElapsedMilliseconds < 1750)
                continue; 
        }

        Debug.LogWarning("IRCOutput Thread (Send) exited");
    }

    private void HandleRPL(string type)
    {
        switch (type)
        {
            case "001":
                SendCommand("JOIN #" + details.channel.ToLower());
                ConnectionStateAlert(StatusType.Success, "Connected to Twitch IRC. Now joining channel: " + details.channel + "...", 100);
                break;
            case "353":
                Debug.Log("<color=#bd2881><b>[JOIN]</b></color> Joined channel: " + details.channel + " successfully");
                break;
        }
    }

    private void HandlePRIVMSG(string ircString, string tagString)
    {
        // Parse PRIVMSG
        IRCPrivmsg privmsg = new IRCPrivmsg(
            ParseHelper.ParseLoginName(ircString),
            ParseHelper.ParseChannel(ircString),
            ParseHelper.ParseMessage(ircString)
        );

        // Parse Tags
        IRCTags tags = ParseHelper.ParseTags(tagString, settings.parseBadges, settings.parseTwitchEmotes);

        // Sort emotes to match emote order with the chat message (compares emote indexes)
        if (tags.emotes.Count > 0)
            tags.emotes.Sort((a, b) => 1 * a.indexes[0].startIndex.CompareTo(b.indexes[0].startIndex));

        // Send chatter object to listeners
        // Invoke in main thread
        MainThread.Instance.Enqueue(() => newChatMessageEvent.Invoke(new Chatter(privmsg, tags)));
    }
    private void HandleUSERSTATE(string ircString, string tagString)
    {
        // Parse USERSTATE
        IRCUserstate userstate = new IRCUserstate(ParseHelper.ParseChannel(ircString));

        // Parse Tags
        IRCTags tags = ParseHelper.ParseTags(tagString, settings.parseBadges, settings.parseTwitchEmotes);

        userChatter = new Chatter(userstate, tags);
    }

    public void SendCommand(string command, bool instant = false)
    {
        if (instant) //Instant priority (mainly for PING responses)
            stream.WriteLine(command, settings.debugIRC);
        else
            outputQueue.Enqueue(command); // Place command in queue
    }

    /// <summary>
    /// Sends a chat message
    /// </summary>
    public void SendChatMessage(string message)
    {
        if (message.Length <= 0) // Message can't be empty
            return;

        outputQueue.Enqueue("PRIVMSG #" + details.channel + " :" + message); // Place message in queue
    }
}