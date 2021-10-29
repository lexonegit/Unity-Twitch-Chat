# Unity-Twitch-Chat

This is a lightweight [Twitch.tv IRC](https://dev.twitch.tv/docs/irc/) client for Unity.

Unity Twitch Chat allows you to integrate Twitch Chat with your Unity projects.

This solution uses multithreading to send/receive messages to/from Twitch using IRC. Received messages are queued into the main thread and propagated through the client's ChatMessageEvent.

**Note:** Only normal chat messages are currently supported. Whispers, subscriber messages, etc are not implemented. WebGL is not supported.

### Chat message parsing sample

<img src="https://i.imgur.com/KIA8KcZ.png">

## Requirements
1. Twitch Account
2. Twitch OAuth token (You can generate one at https://twitchapps.com/tmi/)

## Instructions

**I recommend looking at the included ExampleProject for a better understanding**

1. Create a new empty GameObject and add **TwitchIRC.cs** to it
3. Enter your Twitch details (OAuth, nick, channel) inside the inspector
4. Create a new empty GameObject and a new C# script with a reference to the **TwitchIRC.cs** component
5. In your new C# script, add a listener to **TwitchIRC.ChatMessageEvent**

## API

#### TwitchIRC.cs
- **TwitchIRC.Connect()** -> Connects to Twitch IRC
- **TwitchIRC.Disconnect()** -> Disconnects from Twitch IRC
- **TwitchIRC.SendChatMessage(string)** -> Sends a Twitch chat message
- **TwitchIRC.ChatMessageEvent** -> Passes Twitch chat messages to listeners

#### Chatter.cs
- **Chatter.GetRGBAColor()** -> Returns chatter's name color in RGBA format
- **Chatter.IsDisplayNameFontSafe()** -> Returns true if chatter's displayName is "font safe" meaning that it only contains characters: a-z, A-Z, 0-9, _
- **Chatter.MessageContainsEmote(string id)** -> Returns true if chat message contains a specific emote (id)
- **Chatter.HasBadge(string name)** -> Returns true if chatter has a specific badge

## Projects made with Unity Twitch Chat
Rocket Chat, live stream MMO minigame https://joshgrrro.com/rocketchat
Intro Fighters, stream overlay game https://lexone.itch.io/introfighters

*Did you create something? Contact the contributors to get featured here.*
