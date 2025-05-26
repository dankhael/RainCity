using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 100f;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    
    [Header("Health System")]
    public int maxHealth = 100;
    public float invincibilityDuration = 1.5f;
    public float damageFlashDuration = 0.1f;
    public Color damageFlashColor = Color.red;
    
    [Header("Fall Detection")]
    public float fallDeathY = -20f; // Y position where player dies from falling
    public float fallDamageThreshold = 15f; // Minimum fall velocity to take damage
    public int fallDamageAmount = 20;
    
    [Header("UI References")]
    public UnityEngine.UI.Slider healthBar; // Optional health bar reference
    public UnityEngine.UI.Text healthText;  // Optional health text reference
    
    [Header("Audio")]
    public AudioClip damageSound;
    public AudioClip deathSound;
    public AudioClip jumpSound;
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    // Health and damage variables
    private int currentHealth;
    private bool isInvincible = false;
    private bool isDead = false;
    private Color originalColor;
    
    // Fall detection variables
    private float previousY;
    private float fallStartY;
    private bool isFalling = false;

    // Existing variables
    private Rigidbody2D rb;
    private Vector2 movementInput;
    private bool isJumpPressed = false;
    private bool isGrounded;
    private bool isSprinting = false;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    // Input System references
    private PlayerInput playerInput;
    private InputAction sprintAction;

    // Events for health changes
    public System.Action<int, int> OnHealthChanged; // current, max
    public System.Action OnPlayerDeath;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();
        audioSource = GetComponent<AudioSource>();
        
        // Store original sprite color
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        // Get the sprint action reference
        if (playerInput != null)
        {
            sprintAction = playerInput.actions["Sprint"];
        }
    }

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Initialize fall detection
        previousY = transform.position.y;
        fallStartY = transform.position.y;
        
        // Subscribe to sprint action events for more reliable detection
        if (sprintAction != null)
        {
            sprintAction.started += OnSprintStarted;
            sprintAction.canceled += OnSprintCanceled;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (sprintAction != null)
        {
            sprintAction.started -= OnSprintStarted;
            sprintAction.canceled -= OnSprintCanceled;
        }
    }

    void Update()
    {
        if (isDead) return; // Don't process input if dead
        
        // Check if grounded
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Check for fall death
        if (transform.position.y <= fallDeathY)
        {
            Die("Fell to death");
            return;
        }

        // Update animator parameters
        bool isMoving = Mathf.Abs(movementInput.x) > 0.01f;
        animator.SetBool("isRunning", isMoving);
        animator.SetBool("isJumping", !isGrounded);
        
        // Add sprinting animation parameter if you have one
        if (animator.parameters.Length > 0)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "isSprinting")
                {
                    animator.SetBool("isSprinting", isSprinting && isMoving);
                    break;
                }
            }
        }

        // Handle sprite flipping
        if (movementInput.x > 0.01f) 
            spriteRenderer.flipX = false;
        else if (movementInput.x < -0.01f) 
            spriteRenderer.flipX = true;

        // Debug current state (optional)
        if (showDebugLogs)
        {
            DebugCurrentState();
        }
    }

    void FixedUpdate()
    {
        if (isDead) return; // Don't move if dead
        
        // Apply movement with current speed
        float currentSpeed = (isSprinting && isGrounded) ? runSpeed : moveSpeed;
        rb.linearVelocity = new Vector2(movementInput.x * currentSpeed, rb.linearVelocity.y);

        // Handle jumping
        if (isJumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Reset jump flag
        isJumpPressed = false;
    }

    #region Health System

    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (showDebugLogs)
        {
            Debug.Log($"<color=orange>Player took {damage} damage. Health: {currentHealth}/{maxHealth}</color>");
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    

        // Trigger damage animation if available
        if (animator != null)
        {
            animator.SetTrigger("takeDamage");
        }

        if (currentHealth <= 0)
        {
            Die("Health depleted");
        }
    }

    public void Heal(int healAmount)
    {
        if (isDead) return;

        int oldHealth = currentHealth;
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (showDebugLogs && currentHealth != oldHealth)
        {
            Debug.Log($"<color=green>Player healed for {currentHealth - oldHealth}. Health: {currentHealth}/{maxHealth}</color>");
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die(string cause)
    {
        if (isDead) return;

        isDead = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"<color=red>Player died: {cause}</color>");
        }

        // Disable player input
        if (playerInput != null)
            playerInput.enabled = false;

        // Stop movement
        rb.linearVelocity = Vector2.zero;

        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger("death");
        }

        OnPlayerDeath?.Invoke();

        // Start death sequence
        StartCoroutine(HandleDeath());
    }

    private IEnumerator HandleDeath()
    {
        // Wait for death animation or a fixed time
        yield return new WaitForSeconds(2f);
        
        // Restart the current scene
        RestartGame();
    }

    public void RestartGame()
    {
        if (showDebugLogs)
        {
            Debug.Log("<color=yellow>Restarting game...</color>");
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    #endregion


    #region Input System Callbacks

    void OnMove(InputValue value)
    {
        if (!isDead)
            movementInput = value.Get<Vector2>();
    }

    void OnJump()
    {
        if (!isDead)
            isJumpPressed = true;
    }

    void OnSprint(InputValue value)
    {
        if (isDead) return;
        
        bool newSprintState = value.isPressed;
        
        if (isSprinting != newSprintState)
        {
            isSprinting = newSprintState;
            
            if (showDebugLogs)
            {
                if (isSprinting)
                {
                    Debug.Log("<color=green>OnSprint Callback: PRESSED</color> - Sprinting state is now TRUE.");
                }
                else
                {
                    Debug.Log("<color=red>OnSprint Callback: RELEASED</color> - Sprinting state is now FALSE.");
                }
            }
        }
    }

    void OnAttack()
    {
        if (isDead) return;
        
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }
        
        // Trigger shooting system
        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.TriggerAttack();
        }
    }

    #endregion

    #region Direct Input Action Callbacks

    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        if (!isDead)
        {
            isSprinting = true;
            
            if (showDebugLogs)
            {
                Debug.Log("<color=green>Sprint Action: STARTED</color> - Sprinting state is now TRUE.");
            }
        }
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
        
        if (showDebugLogs)
        {
            Debug.Log("<color=red>Sprint Action: CANCELED</color> - Sprinting state is now FALSE.");
        }
    }

    #endregion

    #region Public Methods for External Scripts

    public bool IsDead() => isDead;
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void FullHeal()
    {
        Heal(maxHealth);
    }

    #endregion

    #region Debug and Utility Methods

    private void DebugCurrentState()
    {
        // Only log state changes to avoid spam
        if (Time.frameCount % 60 == 0 && showDebugLogs) // Every second at 60 FPS
        {
            Debug.Log($"Player State - Moving: {Mathf.Abs(movementInput.x) > 0.01f}, " +
                     $"Sprinting: {isSprinting}, " +
                     $"Grounded: {isGrounded}, " +
                     $"Health: {currentHealth}/{maxHealth}, " +
                     $"Falling: {isFalling}, " +
                     $"Current Speed: {(isSprinting && isGrounded ? runSpeed : moveSpeed)}");
        }
    }

    [ContextMenu("Toggle Sprint")]
    public void ToggleSprint()
    {
        if (!isDead)
        {
            isSprinting = !isSprinting;
            Debug.Log($"Sprint manually toggled to: {isSprinting}");
        }
    }

    [ContextMenu("Take Damage (10)")]
    public void DebugTakeDamage()
    {
        TakeDamage(10);
    }

    [ContextMenu("Heal (20)")]
    public void DebugHeal()
    {
        Heal(20);
    }

    [ContextMenu("Kill Player")]
    public void DebugKill()
    {
        TakeDamage(currentHealth);
    }

    public bool IsCurrentlySprinting()
    {
        return isSprinting && Mathf.Abs(movementInput.x) > 0.01f && isGrounded && !isDead;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        // Draw fall death line
        Gizmos.color = Color.red;
        Vector3 leftPoint = new Vector3(transform.position.x - 10f, fallDeathY, 0);
        Vector3 rightPoint = new Vector3(transform.position.x + 10f, fallDeathY, 0);
        Gizmos.DrawLine(leftPoint, rightPoint);
        
        // Draw fall damage threshold indicator
        if (isFalling)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, fallStartY, 0), 0.5f);
        }
    }

    #endregion
}