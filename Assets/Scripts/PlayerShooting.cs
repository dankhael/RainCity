using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public Transform shootPoint; // Where projectiles spawn from
    public float projectileSpeed = 15f;
    public float shootCooldown = 0.5f;
    
    [Header("Audio (Optional)")]
    public AudioClip shootSound;
    
    private float lastShootTime;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    
    // Animation event flag
    private bool shouldShoot = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        // Create audio source if none exists
        if (audioSource == null && shootSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        // Handle shooting when animation event triggers
        if (shouldShoot)
        {
            shouldShoot = false;
            ShootProjectile();
        }
    }

    // Call this method from your PlayerController's OnAttack()
    public void TriggerAttack()
    {
        if (CanShoot())
        {
            shouldShoot = true;
            lastShootTime = Time.time;
        }
    }

    // This method is called by Animation Event
    public void OnShootAnimationEvent()
    {
        ShootProjectile();
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || shootPoint == null) return;

        // Determine shoot direction based on sprite flip
        Vector2 shootDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        
        // Instantiate projectile
        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        
        // Initialize projectile
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(shootDirection, projectileSpeed);
        }
        
        // Play sound effect
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    private bool CanShoot()
    {
        return Time.time >= lastShootTime + shootCooldown;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (shootPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shootPoint.position, 0.1f);
            
            // Draw shoot direction
            Vector2 direction = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
            Gizmos.DrawRay(shootPoint.position, direction * 2f);
        }
    }
}