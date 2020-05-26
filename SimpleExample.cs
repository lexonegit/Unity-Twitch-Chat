using UnityEngine;

public class SimpleExample : MonoBehaviour
{
    private TwitchIRC IRC;
    public Chatter latestChatter;

    private void Awake()
    {
        // Place TwitchIRC.cs script on an gameObject called "TwitchIRC"
        IRC = GameObject.Find("TwitchIRC").GetComponent<TwitchIRC>();

        // Add an event listener
        IRC.newChatMessageEvent.AddListener(NewMessage);
    }

    // This gets called whenever a new chat message appears
    public void NewMessage(Chatter chatter)
    {
        Debug.Log("New chatter object received! " + chatter.tags.displayName);

        // Some examples on how you could use the chatter objects

        if (chatter.tags.displayName == "Lexone")
            Debug.Log("Chat message was sent by Lexone!");

        if (chatter.HasBadge("subscriber"))
            Debug.Log("Chat message sender is a subscriber");

        if (chatter.HasBadge("moderator"))
            Debug.Log("Chat message sender is a channel moderator");

        if (chatter.MessageContainsEmote("25")) //25 = Kappa emote ID
            Debug.Log("Chat message contained the Kappa emote");

        if (chatter.message == "!join")
            Debug.Log(chatter.tags.displayName + " said !join");

        // Get chatter's name color (RGBA Format)
        //
        Color nameColor = chatter.GetRGBAColor();

        // Check chatter's display name for unusual characters
        //
        // This can be useful to check for because most fonts don't support unusual characters
        // If that's the case then you could use their login name instead (chatter.login) or use a fallback font
        // Login name is always lowercase and can only contain characters: a-z, A-Z, 0-9, _
        //
        if (chatter.CheckDisplayName())
            Debug.Log("Chatter's displayName contains characters other than a-z, A-Z, 0-9, _");





        latestChatter = chatter;
    }
}
