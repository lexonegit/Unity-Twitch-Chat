# Unity-Twitch-Chat

This is a lightweight [Twitch.tv IRC](https://dev.twitch.tv/docs/irc/) client for Unity. Multithreading is used for reading chat output.

Unity Twitch Chat allows you to integrate Twitch Chat to your Unity projects.

**NOTICE:** 
- Only normal chat messages are currently supported. Whispers, subscriber messages, etc are not implemented. 
- WebGL is not supported.

### Chat message parsing sample

<img src="https://i.imgur.com/KIA8KcZ.png">

## Requirements
1. Twitch Account
2. Twitch OAuth token (You can generate one at https://twitchapps.com/tmi/)

## Quick start

1. Place the **TwitchIRC** folder into your Project's Assets folder
2. Add the **TwitchIRC** prefab into your scene (TwitchIRC/Prefabs/TwitchIRC.prefab)
3. Enter your Twitch details in the inspector of **TwitchIRC** (nick should be your username)
4. Add the **ExampleIRCListener** prefab into your scene (TwitchIRC/Prefabs/ExampleIRCListener.prefab)
5. Hit play! TwitchIRC should connect to your chosen Twitch channel and start listening to messages
6. Create your own listener scripts to fit your needs (look at the ExampleIRCListener.cs script for help and examples)

*Having issues? I recommend looking at the included ExampleProject for a better understanding* 

## Included example project (Unity 2020.3.20f1 LTS)

Spawns chatters as jumping rigidbody boxes

![example_project_gif](https://user-images.githubusercontent.com/18125997/144342000-f506e491-6e51-434d-ac7a-b17be2837922.gif)



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
