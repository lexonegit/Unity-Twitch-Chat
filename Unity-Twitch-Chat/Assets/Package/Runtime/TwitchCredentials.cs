using UnityEngine;

namespace Incredulous.Twitch
{

    [System.Serializable]
    public struct TwitchCredentials
    {
        [Tooltip("The Twitch username which will be used to authenticate with Twitch.")]
        public string username;

        [Tooltip("The OAuth key which will be used to authenticate with Twitch. Generate an OAuth key at https://twitchapps.com/tmi/.")]
        public string oauth;

        [Tooltip("The Twitch channel which the chat client will join.")]
        public string channel;
    }

}