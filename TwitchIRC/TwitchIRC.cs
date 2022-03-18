using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using UnityEngine;

/// <summary>
/// Twitch.tv IRC client for Unity https://github.com/lexonegit/Unity-Twitch-Chat
/// </summary>
public class TwitchIRC : MonoBehaviour
{
    [HideInInspector] public class NewChatMessageEvent : UnityEngine.Events.UnityEvent<Chatter> { }
    [HideInInspector] public class StatusEvent : UnityEngine.Events.UnityEvent<StatusType, string, int> { }

    // Events
    public NewChatMessageEvent newChatMessageEvent = new NewChatMessageEvent(); // New chat messages
    public StatusEvent statusEvent = new StatusEvent(); // Connection events

    private TcpClient client;
    private NetworkStream stream;

    public string ircAddress = "irc.chat.twitch.tv";
    public int port = 6667;

    public TwitchDetails twitchDetails;
    public Settings settings;

    [Header("Client chatter object")]
    [Tooltip("Contains some information about the client user (OAuth)")] public Chatter clientChatter;

    private bool connected = false;
    private Thread outputThread = null;
    private Thread inputThread = null;

    [System.Serializable]
    public class TwitchDetails
    {
        public string oauth = string.Empty;
        public string nick = string.Empty;
        public string channel = string.Empty;
    }

    [System.Serializable]
    public class Settings
    {
        public bool autoConnectOnStart = true;
        public bool parseBadges = true;
        public bool parseTwitchEmotes = true;
        [Space(12f)]
        public bool debugIRC = true;
    }

    #region Unity MonoBehaviour functions
    private void Start()
    {
        if (settings.autoConnectOnStart)
            StartCoroutine(PrepareConnection());
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnDisable()
    {
        Disconnect();
    }
    #endregion

    [ContextMenu("Connect IRC")]
    public void IRC_Connect() 
    {
        StartCoroutine(PrepareConnection());
    }

    [ContextMenu("Disconnect IRC")]
    public void IRC_Disconnect() 
    {
        Disconnect();
    }

    private IEnumerator PrepareConnection()
    {
        if (inputThread != null && outputThread != null)
            while (inputThread.IsAlive || outputThread.IsAlive) // Wait for previous threads to close (if there are any)
                yield return null;

        if (twitchDetails.oauth.Length <= 0 || twitchDetails.nick.Length <= 0 || twitchDetails.channel.Length <= 0)
        {
            ConnectionStateAlert(StatusType.Error, "Missing required details! Check your Twitch details.");
            yield break;
        }

        // Fix formatting (twitchapps.com)
        if (twitchDetails.oauth.StartsWith("oauth:"))
            twitchDetails.oauth = twitchDetails.oauth.Substring(6);

        ConnectIRC();
    }

    private void ConnectIRC()
    {
        client = new TcpClient(ircAddress, port); // Connect to Twitch IRC
        stream = client.GetStream();

        if (!client.Connected)
        {
            ConnectionStateAlert(StatusType.Error, "Failed connecting to Twitch IRC.");
            return;
        }

        stream.WriteLine("PASS oauth:" + twitchDetails.oauth.ToLower());
        stream.WriteLine("NICK " + twitchDetails.nick.ToLower());
        stream.WriteLine("CAP REQ :twitch.tv/tags twitch.tv/commands");

        connected = true;
        MainThread.Instance.Clear();
        outputQueue.Clear();

        // Initialize threads
        inputThread = new Thread(() => IRCInputProc());
        outputThread = new Thread(() => IRCOutputProc());

        // Start threads
        inputThread.Start();
        outputThread.Start();
    }

    private void Disconnect(bool reconnect = false)
    {
        if (!connected) return;

        connected = false; // Stop threads
        
        client.Close();
        stream.Close();
        
        Debug.LogWarning("Disconnected from Twitch IRC");

        if (reconnect)
            StartCoroutine(PrepareConnection());
    }

    private void IRCInputProc()
    {
        Debug.Log("IRCInput Thread (Receive) started");

        using (StreamReader reader = new StreamReader(stream)) 
        {
            string raw;
            while (connected)
            {
                // try-catch is needed because ReadLine() is a blocking call and disconnecting will cause an exception without it
                try { raw = reader.ReadLine(); }
                catch // Add (System.Exception err) here to debug error messages
                {
                    // ReadLine() was interrupted. Perhaps Disconnect() was called?
                    // ...however sometimes ReadLine() fails with mysterious errors like:
                    //
                    // "System.IO.IOException: Unable to read data from the transport connection: An established connection was aborted by the software in your host machine."
                    // or something else... Not sure why they happen. It is seemingly random.
                    //
                    // When this happens, we are still "connected" so try reconnecting

                    if (connected)
                    {
                        Debug.LogError("Error while reading IRC input. Reconnecting...");
                        MainThread.Instance.Enqueue(() => Disconnect(true)); // Disconnect, but then reconnect
                    }

                    break; // Stop this thread loop
                }

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
                        case "PRIVMSG": // Chat message
                            HandlePRIVMSG(ircString, tagString);
                            break;
                        case "USERSTATE": // Userstate
                            HandleUSERSTATE(ircString, tagString);
                            break;
                        case "353": // Successful channel join
                            HandleRPL(type);
                            break;
                        case "001": // Successful IRC connection
                            HandleRPL(type);
                            break;
                    }
                }

                // Respond to PING messages
                if (raw.StartsWith("PING"))
                {
                    SendCommand("PONG :tmi.twitch.tv", true);
                }
            }
        }

        Debug.LogWarning("IRCInput Thread (Receive) exited");
    }

    private Queue<string> outputQueue = new Queue<string>();
    private void IRCOutputProc()
    {
        Debug.Log("IRCOutput Thread (Send) started");

        System.Diagnostics.Stopwatch cooldown = new System.Diagnostics.Stopwatch();
        
        // Read loop
        while (connected)
        {
            if (outputQueue.Count <= 0)
                continue;

            // Send next output from outputQueue
            stream.WriteLine(outputQueue.Dequeue(), settings.debugIRC);

            // Cooldown timer for avoiding Twitch IRC rate limits
            // https://dev.twitch.tv/docs/irc/guide#rate-limits
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
                SendCommand("JOIN #" + twitchDetails.channel.ToLower());
                ConnectionStateAlert(StatusType.Success, "Connected to Twitch IRC. Now joining channel: " + twitchDetails.channel + "...", 100);
                break;
            case "353":
                Debug.Log("<color=#bd2881><b>[JOIN]</b></color> Joined channel: " + twitchDetails.channel + " successfully");
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

        clientChatter = new Chatter(userstate, tags);
    }

    public void SendCommand(string command, bool instant = false)
    {
        if (instant) // Instant priority (intended for PING responses)
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

        outputQueue.Enqueue("PRIVMSG #" + twitchDetails.channel + " :" + message); // Place message in queue
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
}