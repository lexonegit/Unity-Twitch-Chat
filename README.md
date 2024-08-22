# Unity Twitch Chat

This is a lightweight and efficient [Twitch.tv IRC](https://dev.twitch.tv/docs/irc/) client for Unity.

In short, this allows you to integrate Twitch Chat to your Unity projects.<br>The primary goal is to be able to read and send chat messages as efficiently as possible.



## Chat message example
<img src="https://user-images.githubusercontent.com/18125997/210407885-b2c49e1e-4537-41e9-ad5b-2d8c4c8f1077.png">

### Supported features
- Reading and sending chat messages
- Parsing Twitch emotes and badges
- Optional anonymous login
- Automatic ratelimit checks
- Name color normalization (similar to native Twitch chat)

### Unsupported features
- Special messages (whispers, sub/resub, raids, first time viewers, etc)
- Bits cheering, channel points, predictions, etc
- Moderation (ban, timeout, etc)
- Third party extensions (FFZ, BTTV, 7TV, etc)
- and more...

### Other limitations
- WebGL builds are not supported

## Installation

- Open Unity Package Manager <i>(Window -> Package Manager)</i>
- Click the `+` button in the top left corner
- Select `Add package from git URL...`
- Copy and paste the following URL and finish by clicking `Add`<br>
```
https://github.com/lexonegit/Unity-Twitch-Chat.git?path=/Unity-Twitch-Chat/Assets/Package
```


## Quick start
1. Install the Unity package (see above)
2. Create a new empty GameObject and add the `Twitch IRC` component.
3. In the inspector, set your Twitch details (OAuth, username, channel) 
    - You can generate an OAuth token at https://twitchapps.com/tmi/
    - Alternatively you can enable `Use Anonymous Login` to use without OAuth
4. Make sure `Connect IRC On Start` and `Join Channel On Start` are enabled and press play – You should now see JOIN messages, etc. in the console.
5. Create a new script that has a listener for the `IRC.OnChatMessage` event.
    - See <a href="https://github.com/lexonegit/Unity-Twitch-Chat/blob/main/Unity-Twitch-Chat/Assets/ExampleProject/ListenerExample.cs">ListenerExample.cs</a> for reference.
    -  The listener will receive `Chatter` objects which contain information about each chat message, such as the chatter name, message, emotes, etc...

<i>Having issues? Check out the included ExampleProject for a better understanding.</i>

## Example project
Spawn chatters as jumping boxes. Box color is based on their primary badge.

<img src="https://user-images.githubusercontent.com/18125997/210427322-27d2231c-5123-4785-997e-53838cfc8972.gif">

## API documentation

### IRC.cs
- `void` **Connect()** - Connects to Twitch IRC
- `void` **Disconnect()** - Disconnects from Twitch IRC
- `void` **SendChatMessage(string message)** - Sends a chat message to the channel
- `void` **JoinChannel(string channel)** - Join a Twitch channel
- `void` **LeaveChannel(string channel)** - Leave a Twitch channel
- `void` **Ping()** - Sends a PING message to the Twitch IRC server
- `event` **OnChatMessage** - Event that is invoked when a chat message is received
- `event` **OnConnectionAlert** - Event that is invoked when a connection alert is received
- `IRCTags` **ClientUserTags** - Returns the tags of the client user (badges, name color, etc)

### Chatter.cs
- `Color` **GetNameColor()** - Returns the color of the chatter's name
- `bool` **IsDisplayNameFontSafe()** - Returns true if displayName is "font-safe" meaning that it only contains characters: a-z, A-Z, 0-9, _
- `bool` **ContainsEmote(string emoteId)** - Returns true if the chatter's message contains the specified emote (by id)
- `bool` **HasBadge(string badgeName)** - Returns true if the chatter has the specified badge

## License
<a href="https://github.com/lexonegit/Unity-Twitch-Chat/blob/master/LICENSE">MIT License</a>

## Projects made with Unity Twitch Chat

Intro Fighters, stream overlay game https://lexone.itch.io/introfighters

*Did you make something cool? Contact me (lexone on Discord) to get featured here!*
