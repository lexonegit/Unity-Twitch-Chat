# Unity-Twitch-Chat

This is a lightweight [Twitch.tv IRC](https://dev.twitch.tv/docs/irc/) client for Unity.

Unity Twitch Chat allows you to integrate Twitch Chat to your Unity projects.

This solution uses multithreading and because Unity API isn't thread safe I've also included a script (**MainThread.cs**) that enqueues tasks for the main thread to process.

**Note:** Only normal chat messages are currently supported. Whispers, subscriber messages, etc are not implemented. WebGL is not supported.

### Chat message parsing sample

<img src="https://i.imgur.com/KIA8KcZ.png">

## Requirements
1. Twitch Account
2. Twitch OAuth token (You can generate one at https://twitchapps.com/tmi/)

## Instructions

**I recommend looking at the included ExampleProject for a better understanding**

1. Add the **MainThread prefab** into your scene *Assets/MainThread/MainThread.prefab*
2. Create a new empty GameObject and add **TwitchIRC.cs** on it
3. Enter your Twitch details (OAuth, nick, channel) inside the inspector
4. Create a new empty GameObject and a new C# script with an reference to the **TwitchIRC.cs** component
5. In the new C# script, add a listener to **TwitchIRC.newChatMessageEvent**

## Included example project

Spawn chatters as jumping rigidbody boxes

<img src="https://i.imgur.com/QIh9g2D.gif" width="70%">

## API

#### TwitchIRC.cs
- **TwitchIRC.IRC_Connect()** -> Connects to Twitch IRC
- **TwitchIRC.IRC_Disconnect()** -> Disconnects from Twitch IRC
- **TwitchIRC.SendChatMessage(string)** -> Sends a Twitch chat message

#### Chatter.cs
- **Chatter.GetRGBAColor()** -> Returns chatter's name color in RGBA format
- **Chatter.IsDisplayNameFontSafe()** -> Returns true if chatter's displayName is "font safe" meaning that it only contains characters: a-z, A-Z, 0-9, _
- **Chatter.MessageContainsEmote(string id)** -> Returns true if chat message contains a specific emote (id)
- **Chatter.HasBadge(string name)** -> Returns true if chatter has a specific badge

## Projects made with Unity Twitch Chat
Intro Fighters, stream overlay game https://lexone.itch.io/introfighters

*Did you create something? Contact me to get featured here.*
