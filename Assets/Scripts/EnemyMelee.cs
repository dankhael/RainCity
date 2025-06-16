using UnityEngine;

// Certifique-se de que sua interface IDamageable do Projectile.cs esteja no escopo
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Attack Settings")]
    [Tooltip("Raio em torno deste inimigo para detectar e atingir o jogador.")]
    public float attackRange = 1.5f;
    [Tooltip("Quais camadas contam como o jogador.")]
    public LayerMask playerLayer;
    [Tooltip("Dano causado quando o ataque se conecta.")]
    public int damage = 1;
    [Tooltip("Segundos entre ataques.")]
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("Movement Settings")] // NOVAS CONFIGURAÇÕES DE MOVIMENTO
    [Tooltip("Velocidade de movimento do inimigo.")]
    public float moveSpeed = 2f;
    [Tooltip("Objeto vazio para verificar se o inimigo está no chão.")]
    public Transform groundCheck;
    [Tooltip("Raio para o check de chão.")]
    public float groundCheckRadius = 0.2f;
    [Tooltip("Camada do chão.")]
    public LayerMask groundLayer;
    [Tooltip("Objeto vazio para verificar a borda da plataforma na direção do movimento.")]
    public Transform edgeCheck;
    [Tooltip("Distância para o check de borda. Deve ser um pouco maior que o groundCheckRadius.")]
    public float edgeCheckDistance = 0.5f;

    [Header("Optional References")]
    [Tooltip("Atribua se você não quiser usar a pesquisa pela tag Player.")]
    public Transform player;

    private Animator animator;
    private Rigidbody2D rb; // NOVA REFERÊNCIA PARA RIGIDBODY2D
    private Vector2 moveDirection; // NOVA VARIÁVEL PARA A DIREÇÃO DE MOVIMENTO

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>(); // OBTÉM O RIGIDBODY2D

        // Se você não atribuiu uma referência ao jogador, tente encontrar uma pela tag:
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        // Garante que o groundCheck e edgeCheck estão configurados
        if (groundCheck == null) Debug.LogError("GroundCheck não atribuído no Enemy script.");
        if (edgeCheck == null) Debug.LogError("EdgeCheck não atribuído no Enemy script.");
    }

    void Update()
    {
        if (player == null) return;

        // Lógica de ataque (existente)
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }

        // --- NOVA LÓGICA DE MOVIMENTO E DETECÇÃO DE BORDA ---
        // Determina a direção para o jogador
        float directionToPlayer = Mathf.Sign(player.position.x - transform.position.x);

        // Define a direção de movimento inicial baseada no jogador
        moveDirection = new Vector2(directionToPlayer, 0);

        // Verifica o chão
        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Se não estiver no chão, não se move para evitar cair
        if (!isGrounded)
        {
            moveDirection = Vector2.zero; // Para de se mover
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Garante que a velocidade horizontal seja zero
        }
        else
        {
            // Verifica se há uma borda à frente (na direção do movimento)
            // Lança um raio do edgeCheck para baixo
            RaycastHit2D hit = Physics2D.Raycast(edgeCheck.position, Vector2.down, edgeCheckDistance, groundLayer);

            // Se o raio não atingir nada OU se o inimigo está se movendo em direção à borda
            if (hit.collider == null)
            {
                // Se a borda é detectada na direção que o inimigo está indo, ele para ou vira
                // Neste caso, vamos fazê-lo parar de se mover horizontalmente
                moveDirection = Vector2.zero;
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }

        // Aplica o movimento se não estiver atacando ou detectando borda
        if (moveDirection.x != 0 && Vector2.Distance(transform.position, player.position) > attackRange)
        {
            rb.linearVelocity = new Vector2(moveDirection.x * moveSpeed, rb.linearVelocity.y);
            // Vira o sprite do inimigo na direção do movimento
            GetComponent<SpriteRenderer>().flipX = moveDirection.x < 0;
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Para o movimento horizontal
        }
    }

    void Attack()
    {
        // Dispara sua animação de ataque
        animator.SetTrigger("attack");
        rb.linearVelocity = Vector2.zero; // Para o inimigo ao atacar
    }

    // Adicione isso como um Evento de Animação em sua animação de ataque no frame em que você deseja causar dano
    public void OnAttackHit()
    {
        // Detecta quaisquer colliders do jogador no alcance
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, playerLayer);
        foreach (var col in hits)
        {
            var dmg = col.GetComponent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(damage);
        }
    }

    // Implementação IDamageable – chamada pelo seu Projectile
    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;

        // Reação de acerto opcional
        animator.SetTrigger("hurt");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        // Toca a animação de morte, desabilita mais comportamento e depois destrói
        animator.SetTrigger("death");
        enabled = false;
        GetComponent<Collider2D>().enabled = false; // Desabilita o collider para que o inimigo "caia" ou não interaja mais
        rb.gravityScale = 1; // Garante que a gravidade esteja ativada para a morte
        Destroy(gameObject, 0.5f);
    }

    // Visualiza o alcance de ataque, ground check e edge check no editor
    void OnDrawGizmosSelected()
    {
        // Alcance de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Ground check
        if (groundCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Edge check
        if (edgeCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(edgeCheck.position, new Vector3(edgeCheck.position.x, edgeCheck.position.y - edgeCheckDistance, edgeCheck.position.z));
            Gizmos.DrawWireSphere(new Vector3(edgeCheck.position.x, edgeCheck.position.y - edgeCheckDistance, edgeCheck.position.z), 0.1f);
        }
    }
}