using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifetime = 15f;
    public int damage = 1;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public LayerMask hitLayers = -1; // What can this projectile hit
    
    private Rigidbody2D rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 direction, float projectileSpeed = -1)
    {
        // Use custom speed if provided, otherwise use default
        float finalSpeed = projectileSpeed > 0 ? projectileSpeed : speed;
        
        // Set velocity
        rb.linearVelocity = direction.normalized * finalSpeed;
        
        // Rotate to face direction of travel
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Prevent multiple hits
        if (hasHit) return;
        
        // Check if we hit something on the specified layers
        if (((1 << other.gameObject.layer) & hitLayers) != 0)
        {
            hasHit = true;
            
            // Try to damage the target
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }
            
            // Spawn hit effect
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy projectile
            Destroy(gameObject);
        }
    }

    void OnBecameInvisible()
    {
        // Clean up projectile when it goes off screen
        if (gameObject != null)
        {
            Destroy(gameObject, 1f);
        }
    }
}

// Simple damage interface for consistency
public interface IDamageable
{
    void TakeDamage(int damage);
}