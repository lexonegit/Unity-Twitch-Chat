# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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