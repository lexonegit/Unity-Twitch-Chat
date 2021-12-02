using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    private TwitchIRC IRC;
    public GameObject boxPrefab;
    public int maxBoxCount = 100;
    private int spawnCount = 0;

    private void Start()
    {
        // Place TwitchIRC.cs script on an gameObject called "TwitchIRC"
        IRC = GameObject.Find("TwitchIRC").GetComponent<TwitchIRC>();

        // Add an event listener
        IRC.newChatMessageEvent.AddListener(NewMessage);
    }

    // This gets called whenever a new chat message appears
    public void NewMessage(Chatter chatter)
    {
        if (spawnCount >= maxBoxCount) 
        {
            Debug.Log("MAX COUNT REACHED! CANNOT SPAWN ANY MORE BOXES!");
            return;
        }

        Debug.Log("New chatter object received! Chatter name: " + chatter.tags.displayName);

        GameObject o = Instantiate(boxPrefab, Random.insideUnitCircle * 3, Quaternion.identity);
        o.GetComponent<BoxController>().Initialize(chatter);

        spawnCount++;
    }
}
