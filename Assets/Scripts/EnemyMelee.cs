using UnityEngine;

// Ensure your Projectile.cs IDamageable interface is in scope
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Attack Settings")]
    [Tooltip("Radius around this enemy to detect and hit the player.")]
    public float attackRange = 1.5f;
    [Tooltip("Which layers count as the player.")]
    public LayerMask playerLayer;
    [Tooltip("Damage dealt when attack connects.")]
    public int damage = 1;
    [Tooltip("Seconds between attacks.")]
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("Optional References")]
    [Tooltip("Assign if you don't want to use the Player tag lookup.")]
    public Transform player;

    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();

        // If you didn't assign a player reference, try to find one by tag:
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Check attack cooldown
        if (Time.time < lastAttackTime + attackCooldown) return;

        // Only attack when the player is within range
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    void Attack()
    {
        // Trigger your attack animation
        animator.SetTrigger("attack");
    }

    // Add this as an Animation Event on your attack animation at the frame you want to deal damage
    public void OnAttackHit()
    {
        // Detect any player colliders in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, playerLayer);
        foreach (var col in hits)
        {
            var dmg = col.GetComponent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(damage);
        }
    }

    // IDamageable implementation – called by your Projectile
    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        // Optional hit reaction
        animator.SetTrigger("hurt");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        // Play death animation, disable further behavior, then destroy
        animator.SetTrigger("death");
        enabled = false;
        Destroy(gameObject, 0.5f);
    }

    // Visualize attack range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
