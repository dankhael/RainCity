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
    
    [Header("Breath/Stamina System")]
    public float maxBreath = 100f;
    public float breathDrainRate = 25f; // How fast breath drains while sprinting
    public float breathRecoveryRate = 35f; // How fast breath recovers while not sprinting
    public float breathRecoveryDelay = 1f; // Delay before breath starts recovering after sprinting
    public float minBreathToSprint = 10f; // Minimum breath needed to start sprinting
    public bool enableBreathSystem = true; // Toggle to enable/disable the system
    
    [Header("Fall Detection")]
    public float fallDeathY = -20f; // Y position where player dies from falling
    public float fallDamageThreshold = 15f; // Minimum fall velocity to take damage
    public int fallDamageAmount = 20;
    
    [Header("UI References")]
    public UnityEngine.UI.Slider healthBar; // Optional health bar reference
    public UnityEngine.UI.Text healthText;  // Optional health text reference
    public UnityEngine.UI.Slider breathBar; // Breath/Stamina bar reference
    public UnityEngine.UI.Text breathText;  // Optional breath text reference
    public UnityEngine.UI.Image breathBarFill; // Optional reference to change colors
    
    [Header("Breath UI Colors")]
    public Color breathFullColor = Color.green;
    public Color breathMidColor = Color.yellow;
    public Color breathLowColor = Color.red;
    public Color breathEmptyColor = Color.gray;
    
    [Header("Audio")]
    public AudioClip damageSound;
    public AudioClip deathSound;
    public AudioClip jumpSound;
    public AudioClip breathingHeavySound; // Optional heavy breathing sound when low on breath
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    // Health and damage variables
    private int currentHealth;
    private bool isInvincible = false;
    private bool isDead = false;
    private Color originalColor;
    
    // Breath/Stamina variables
    private float currentBreath;
    private bool canSprint = true;
    private float lastSprintTime = 0f;
    private bool isRecoveringBreath = false;
    
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
    private bool wantsToSprint = false; // Tracks if player is trying to sprint

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    // Input System references
    private PlayerInput playerInput;
    private InputAction sprintAction;

    // Events for health and breath changes
    public System.Action<int, int> OnHealthChanged; // current, max
    public System.Action<float, float> OnBreathChanged; // current, max
    public System.Action OnPlayerDeath;
    public System.Action OnBreathDepleted; // When breath runs out
    public System.Action OnBreathRecovered; // When breath is fully recovered

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
        
        // Initialize breath
        currentBreath = maxBreath;
        
        // Initialize fall detection
        previousY = transform.position.y;
        fallStartY = transform.position.y;
        
        // Subscribe to sprint action events for more reliable detection
        if (sprintAction != null)
        {
            sprintAction.started += OnSprintStarted;
            sprintAction.canceled += OnSprintCanceled;
        }
        
        // Initialize UI
        UpdateBreathUI();
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

        // Handle breath system
        HandleBreathSystem();

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

    #region Breath/Stamina System

    private void HandleBreathSystem()
    {
        if (!enableBreathSystem) return;

        bool isMoving = Mathf.Abs(movementInput.x) > 0.01f;
        bool shouldBeSprinting = wantsToSprint && isMoving && isGrounded && canSprint;

        // Check if we can start sprinting
        if (wantsToSprint && !isSprinting && currentBreath >= minBreathToSprint)
        {
            canSprint = true;
        }
        else if (currentBreath < minBreathToSprint)
        {
            canSprint = false;
        }

        // Update actual sprinting state
        isSprinting = shouldBeSprinting;

        // Handle breath consumption and recovery
        if (isSprinting && isMoving)
        {
            // Drain breath while sprinting
            currentBreath -= breathDrainRate * Time.deltaTime;
            currentBreath = Mathf.Clamp(currentBreath, 0f, maxBreath);
            
            lastSprintTime = Time.time;
            isRecoveringBreath = false;

            // Stop sprinting if out of breath
            if (currentBreath <= 0f)
            {
                canSprint = false;
                OnBreathDepleted?.Invoke();
                
                if (showDebugLogs)
                {
                    Debug.Log("<color=red>Out of breath! Cannot sprint.</color>");
                }
            }
        }
        else
        {
            // Recover breath when not sprinting
            float timeSinceLastSprint = Time.time - lastSprintTime;
            
            if (timeSinceLastSprint >= breathRecoveryDelay)
            {
                if (!isRecoveringBreath)
                {
                    isRecoveringBreath = true;
                }
                
                float oldBreath = currentBreath;
                currentBreath += breathRecoveryRate * Time.deltaTime;
                currentBreath = Mathf.Clamp(currentBreath, 0f, maxBreath);
                
                // Check if fully recovered
                if (oldBreath < maxBreath && currentBreath >= maxBreath)
                {
                    OnBreathRecovered?.Invoke();
                }
            }
        }

        // Update UI
        UpdateBreathUI();
    }

    private void UpdateBreathUI()
    {
        // Update breath bar
        if (breathBar != null)
        {
            breathBar.value = currentBreath / maxBreath;
        }

        // Update breath text
        if (breathText != null)
        {
            breathText.text = $"Breath: {Mathf.RoundToInt(currentBreath)}/{Mathf.RoundToInt(maxBreath)}";
        }

        // Update breath bar color based on current breath level
        if (breathBarFill != null)
        {
            float breathPercentage = currentBreath / maxBreath;
            
            if (breathPercentage > 0.6f)
                breathBarFill.color = breathFullColor;
            else if (breathPercentage > 0.3f)
                breathBarFill.color = breathMidColor;
            else if (breathPercentage > 0f)
                breathBarFill.color = breathLowColor;
            else
                breathBarFill.color = breathEmptyColor;
        }

        // Trigger events
        OnBreathChanged?.Invoke(currentBreath, maxBreath);
    }

    #endregion

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
        
        bool newWantsToSprint = value.isPressed;
        
        if (wantsToSprint != newWantsToSprint)
        {
            wantsToSprint = newWantsToSprint;
            
            if (showDebugLogs)
            {
                if (wantsToSprint)
                {
                    Debug.Log("<color=green>OnSprint Callback: PRESSED</color> - Wants to sprint: TRUE.");
                }
                else
                {
                    Debug.Log("<color=red>OnSprint Callback: RELEASED</color> - Wants to sprint: FALSE.");
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
            wantsToSprint = true;
            
            if (showDebugLogs)
            {
                Debug.Log("<color=green>Sprint Action: STARTED</color> - Wants to sprint: TRUE.");
            }
        }
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        wantsToSprint = false;
        
        if (showDebugLogs)
        {
            Debug.Log("<color=red>Sprint Action: CANCELED</color> - Wants to sprint: FALSE.");
        }
    }

    #endregion

    #region Public Methods for External Scripts

    public bool IsDead() => isDead;
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    
    // Breath system getters
    public float GetCurrentBreath() => currentBreath;
    public float GetMaxBreath() => maxBreath;
    public float GetBreathPercentage() => currentBreath / maxBreath;
    public bool CanSprint() => canSprint && currentBreath >= minBreathToSprint;
    public bool IsRecoveringBreath() => isRecoveringBreath;
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    public void FullHeal()
    {
        Heal(maxHealth);
    }
    
    // Breath system methods
    public void RestoreBreath(float amount)
    {
        currentBreath += amount;
        currentBreath = Mathf.Clamp(currentBreath, 0f, maxBreath);
        UpdateBreathUI();
    }
    
    public void FullRestoreBreath()
    {
        currentBreath = maxBreath;
        canSprint = true;
        UpdateBreathUI();
    }
    
    public void DrainBreath(float amount)
    {
        currentBreath -= amount;
        currentBreath = Mathf.Clamp(currentBreath, 0f, maxBreath);
        if (currentBreath < minBreathToSprint)
        {
            canSprint = false;
        }
        UpdateBreathUI();
    }

    #endregion

    #region Debug and Utility Methods

    private void DebugCurrentState()
    {
        // Only log state changes to avoid spam
        if (Time.frameCount % 60 == 0 && showDebugLogs) // Every second at 60 FPS
        {
            Debug.Log($"Player State - Moving: {Mathf.Abs(movementInput.x) > 0.01f}, " +
                     $"Wants Sprint: {wantsToSprint}, " +
                     $"Actually Sprinting: {isSprinting}, " +
                     $"Can Sprint: {canSprint}, " +
                     $"Grounded: {isGrounded}, " +
                     $"Health: {currentHealth}/{maxHealth}, " +
                     $"Breath: {currentBreath:F1}/{maxBreath}, " +
                     $"Recovering: {isRecoveringBreath}");
        }
    }

    [ContextMenu("Toggle Sprint")]
    public void ToggleSprint()
    {
        if (!isDead)
        {
            wantsToSprint = !wantsToSprint;
            Debug.Log($"Sprint manually toggled to: {wantsToSprint}");
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
    
    [ContextMenu("Drain Breath (25)")]
    public void DebugDrainBreath()
    {
        DrainBreath(25f);
    }
    
    [ContextMenu("Restore Breath (25)")]
    public void DebugRestoreBreath()
    {
        RestoreBreath(25f);
    }
    
    [ContextMenu("Full Restore Breath")]
    public void DebugFullRestoreBreath()
    {
        FullRestoreBreath();
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