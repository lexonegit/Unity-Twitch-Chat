# Twitch IRC for Unity

This is a lightweight [Twitch.tv IRC](https://dev.twitch.tv/docs/irc/) chat client for Unity.

Twitch IRC for Unity allows you to integrate Twitch chat with your Unity projects. This solution uses multithreading to send/receive messages to/from Twitch using IRC. Received messages are queued into the main thread and sent to listeners via a ChatMessageEvent.

**Note:** Only normal chat messages are currently supported. Whispers, subscriber messages, etc are not implemented. WebGL is not supported.

### Chat message parsing sample

<img src="https://i.imgur.com/KIA8KcZ.png">

## Installation

Twitch IRC for Unity can be installed via the package manager. You can install the package directly from its GitHub URL, or from your local disk by downloading the package.

## Requirements
1. Twitch Account
2. Twitch OAuth token (You can generate one at https://twitchapps.com/tmi/)

## Getting Started

1. Create a new empty GameObject and add the **TwitchIRC** component
2. In the inspector, enter your Twitch login information (OAuth, nick, channel)
3. Make sure that "Connect on Start" is checked in the inspector and hit Play– you should see a successful **JOIN** message in your Unity Console!

To start handling chat messages, add a listener to **TwitchIRC.ChatMessageEvent**. The listener will receive a **Chatter** object, which contains informaton about the chat message such as the message itself and the username of the sender.

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
