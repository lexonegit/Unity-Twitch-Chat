using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BoxController : MonoBehaviour
{
    public Text nameText; 
    public SpriteRenderer spriteRenderer;
    public Transform canvasTransform;

    public void Initialize(Chatter chatter)
    {
        // Set chatter's name
        //
        // If chatter's display name is "font safe" then use it. Otherwise use login name.
        // Login name is always lowercase and can only contain characters: a-z, A-Z, 0-9, _
        //
        nameText.text = chatter.IsDisplayNameFontSafe() ? chatter.tags.displayName : chatter.login;

        // Change box color
        //
        if (chatter.HasBadge("moderator"))
            spriteRenderer.color = Color.green; // Green box if chatter has moderator badge
        else
        if (chatter.HasBadge("vip"))
            spriteRenderer.color = Color.magenta; // Magenta box if chatter has VIP badge
        else
        if (chatter.HasBadge("subscriber"))
            spriteRenderer.color = Color.red; // Red box if chatter has subscriber badge
        
        // Detach name canvas from parent so that it doesn't rotate
        canvasTransform.SetParent(null);

        // Start jumping
        StartCoroutine(Jump());
    }

    private void LateUpdate()
    {
        // Update name canvas position each frame
        canvasTransform.position = (Vector2)transform.position + new Vector2(0, 0.8f);
    }

    private IEnumerator Jump()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f));

            float dir = Random.value > .5f ? 1f : -1f; // Random jump direction
            GetComponent<Rigidbody2D>().AddForce(Vector2.up * 10f + (Vector2.right * 3f) * dir, ForceMode2D.Impulse);
        }
    }
}
