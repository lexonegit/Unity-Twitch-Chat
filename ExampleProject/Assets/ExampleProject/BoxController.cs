using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BoxController : MonoBehaviour
{
    public Text nameText; 
    public SpriteRenderer spriteRenderer;
    public Transform canvasTransform;
    
    public Color moderatorColor, vipColor, subscriberColor;

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
        if (chatter.HasBadge("moderator")) // Has moderator badge
            spriteRenderer.color = moderatorColor;
        else
        if (chatter.HasBadge("vip")) // Has VIP badge
            spriteRenderer.color = vipColor;
        else
        if (chatter.HasBadge("subscriber")) // Has subscriber badge
            spriteRenderer.color = subscriberColor;
        
        // Detach name canvas from parent so that it doesn't rotate with the transform
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
