using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lexone.UnityTwitchChat;

public class ListenerExample : MonoBehaviour
{
    public Chatter chatterObject; // Latest chatter object
    public BoxController boxPrefab;
    public int maxBoxes = 100;
    private int spawnCount = 0;

    private void Start()
    {
        // Add a listener for the IRC.OnChatMessage event
        IRC.Instance.OnChatMessage += OnChatMessage;
    }

    private void OnChatMessage(Chatter chatter)
    {
        // Handle new chat messages...

        Debug.Log($"<color=#fef83e><b>[LISTENER EXAMPLE]</b></color> New chat message from {chatter.tags.displayName}");

        // Debug.Log($"Message content: {chatter.message}");
        
        if (spawnCount >= maxBoxes)
        {
            Debug.LogWarning("Max amount of boxes reached!");
            return;
        }

        // Spawn a new box
        Vector3 spawnPosition = Random.insideUnitCircle * 3f;
        BoxController box = Instantiate(boxPrefab, spawnPosition, Quaternion.identity);

        // Initialize the box with the chatter details
        box.Initialize(chatter);

        // This is just to show the latest chatter object in the inspector
        chatterObject = chatter;

        spawnCount++;
    }
}
