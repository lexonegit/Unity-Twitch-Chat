using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lexone.UnityTwitchChat;

public class ConnectionExample : MonoBehaviour
{
    // This is just a simple example on how to use the IRC class methods
    // You could for example hook Unity UI events to these methods

    public void JoinChannel()
    {
        IRC.Instance.JoinChannel("my_channel_name");
    }

    public void LeaveChannel()
    {
        IRC.Instance.LeaveChannel("my_channel_name");
    }
}
