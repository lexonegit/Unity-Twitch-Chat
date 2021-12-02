# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased
### Added
- TwitchIRC.singleton, which when true enables DontDestroyOnLoad and destroys duplicate instances
- TwitchIRC.IsConnected, which is true only while there is a stable connection to Twitch
- TwitchIRC.debugThreads, which allows debug warnings for thread termination to be turned on/off
- Rate limiting
    - RateLimit class
    - Client sets rate limit based on user's channel permissions
    - Client delays messages that exceeed the rate limit
    - Client sends a warning when a message is queued that will exceed the rate limits
- IRCTags.ContainsEmote and IRCTags.HasBadge (previously exclusive to the Chatter object)
- Unity Tooltips for all inspector fields of the TwitchIRC class
### Changed
- Chatter.MessageContainsEmote -> Chatter.ContainsEmote
- Client now uses a standard backoff interval (0s, 1s, 2s, 4s, 8s, ...) when reattempting a failed connection
- Fixed issue where TwitchIRC did not end a failed connection
- Fixed issue where TwitchIRC did not properly handle no internet connection
- Client now checks that the connection still exists in the receive thread instead of a third thread
- On disconnect, pending status updates and chat messages are now propogated before a connection is terminated
- Reduced default write interval to 100ms for faster responsiveness
- Settled on a versioning method for pre-release versions
### Removed
- TwitchIRC.status, which overcomplicated checking the connection for basic cases (for more complex connection handling, listen to the ConnectionAlertEvent)

## [1.0.0] - 2021-11-02
### Added
- TwitchIRC catches a failed authentication attempt
- TwitchIRC checks its connection to the IRC socket on a third thread.
- TwitchIRC.SendPing method (ContextMenu action) for debugging connection
- TwitchIRC.BlockingDisconnect which blocks the main thread while any remainig send/receive threads close (important for closing connecting during OnDisable and OnDestroy)
- TwitchIRC.taskQueue which replaces the functionality of the MainThread class
- XML descriptions for almost all class members
### Changed
- API Changes
    - TwitchIRC.IRC_Connect -> TwitchIRC.Connect
    - TwitchIRC.IRC_Disconnect -> TwitchIRC.Disconnect
    - TwitchIRC.newChatMessageEvent -> TwitchIRC.ChatMessageEvent
    - TwitchIRC.statusEvent -> TwitchIRC.StatusUpdateEvent
- TwitchIRC class
    - Each connection is encapsulated in an internal TwitchConnection object
    - Login messages are sent using the output thread instead of the main thread
    - When disconnecting, waits for threads to terminate before disconnect is completed
    - When connecting, disconnects any existing connection before proceding with a new connection
    - SendCommand no longer sends any messages instantly but uses priorityOutputQueue to send important messages first
    - IRCInputProc decodes buffered data from the socket instead of locking on StreamReader.ReadLine
    - Send/receive threads now sleep for a configurable number of milliseconds before reattempting sends/receives to reduce CPU usage
    - Uses thread safe classes for connected, outputQueue, priorityOutputQueue, and taskQueue
- Versioning in CHANGELOG to reflect that the API was not stable
### Removed
- MainThread class and prefab (functionality has been intergrated into TwitchIRC class)
- IRCPrivMsg class and IRCUserstate class which were mostly unused

## [0.2.0] - 2021-10-26
### Added
- CHANGELOG.md
- package.json
- Lexonegit.UnityTwitchChat.asmdef
### Changed
- All scripts now belong to the Lexonegit.UnityTwitchChat namespace
### Removed
- Sample scene (temporarily)

## [0.1.0] - 2021-06-12
- Last release from lexonegit