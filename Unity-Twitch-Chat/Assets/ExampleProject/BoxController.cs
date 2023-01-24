using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lexone.UnityTwitchChat;

/// <summary>
/// This is an example script that shows some examples on how you could use the information from a Chatter object.
/// </summary>
public class BoxController : MonoBehaviour
{
    public Transform ui;
    public Text nameText;
    public SpriteRenderer spriteRenderer;

    [Header("Badge colors")]
    public Color broadcasterColor;
    public Color moderatorColor;
    public Color vipColor;
    public Color subscriberColor;

    public void Initialize(Chatter chatter)
    {
        // Change name text to chatter's name.
        // Use displayName if it is "font-safe",
        // meaning that it only contains characters: a-z, A-Z, 0-9, _ (most fonts support these characters)
        // If not "font-safe" then use login name instead, which should always be "font-safe"
        nameText.text = chatter.IsDisplayNameFontSafe() ? chatter.tags.displayName : chatter.login;
        nameText.color = chatter.GetNameColor();

        // Change box color to match chatter's primary badge
        if (chatter.HasBadge("broadcaster"))
            spriteRenderer.color = broadcasterColor;
        else
        if (chatter.HasBadge("moderator"))
            spriteRenderer.color = moderatorColor;
        else
        if (chatter.HasBadge("vip"))
            spriteRenderer.color = vipColor;
        else
        if (chatter.HasBadge("subscriber"))
            spriteRenderer.color = subscriberColor;


        // If the chatter's message contained the Kappa emote (emote ID: 25)
        // Then make their box double the size
        if (chatter.ContainsEmote("25"))
        {
            transform.localScale *= 2;
        }


        // Detach UI from parent so that it doesn't rotate with the box
        ui.SetParent(null);

        // Start the jump logic
        StartCoroutine(JumpLogic());
    }

    private void LateUpdate()
    {
        // Update UI position to be above the box
        ui.position = (Vector2)transform.position + new Vector2(0, 1.5f);
    }

    private IEnumerator JumpLogic()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        // Add some random initial force
        rb.AddForce(Random.insideUnitCircle * Random.Range(5f, 10f), ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(-1f, 1f), ForceMode2D.Impulse);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2f, 5f));

            int direction = Random.value > 0.5f ? 1 : -1; // Random jump direction
            Vector2 force = Vector2.up * 10f + (Vector2.right * direction); // Jump force

            rb.AddForce(force, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-1f, 1f), ForceMode2D.Impulse);
        }
    }
}