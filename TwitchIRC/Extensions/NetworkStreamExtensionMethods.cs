using UnityEngine;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Extension methods for the NetworkStream class
/// </summary>
public static class NetworkStreamExtensionMethods
{
    public static void WriteLine(this NetworkStream stream, string output, bool debug = false)
    {
        if (debug)
            Debug.Log("<color=#c91b00><b>[IRC OUTPUT]</b></color> Sending command: " + output);

        byte[] bytes = Encoding.UTF8.GetBytes(output);
        stream.Write(bytes, 0, bytes.Length);
        stream.WriteByte((byte)'\r');
        stream.WriteByte((byte)'\n');
        stream.Flush();
    }
}