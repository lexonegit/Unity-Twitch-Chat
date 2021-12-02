using UnityEngine;

public class ClickExplosion : MonoBehaviour
{
    private Vector2 mousePosition;

    public float explosionStrength = 10f;
    public float explosionRadius = 5f;

    private void Update()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(mousePosition, explosionRadius);
            foreach (Collider2D hit in colliders)
            {
                Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();

                if (rb != null) 
                {
                    Vector2 explosionDir = rb.position - mousePosition;
                    rb.AddForce(explosionDir.normalized * explosionStrength, ForceMode2D.Impulse);
                    rb.AddTorque(Random.Range(-0.5f, 0.5f), ForceMode2D.Impulse);
                }    
            }
        }
    }
}
