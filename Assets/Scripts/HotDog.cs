using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

/// <summary>
/// This script restarts the current level when an object with a specific tag (e.g., "Player")
/// enters its trigger collider.
/// </summary>
public class HotDog : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The tag of the GameObject that will trigger the restart (e.g., 'Player').")]
    public string playerTag = "Player"; // Default tag for the player

    [Header("Feedback (Optional)")]
    [Tooltip("Sound to play when the item is collected/triggered.")]
    public AudioClip pickupSound;
    private AudioSource audioSource;

    [Tooltip("Particle effect to play when the item is collected/triggered.")]
    public ParticleSystem pickupEffect;

    private bool triggered = false; // To prevent multiple triggers

    void Awake()
    {
        // Get or add an AudioSource component for playing sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Called when another Collider2D enters this object's trigger collider.
    /// </summary>
    /// <param name="other">The Collider2D of the object that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if already triggered to avoid multiple restarts from a single touch
        if (triggered)
        {
            return;
        }

        // Check if the colliding object has the specified player tag
        if (other.gameObject.CompareTag(playerTag))
        {
            triggered = true; // Mark as triggered

            Debug.Log($"Restart item touched by: {other.gameObject.name}. Restarting game.");

            // Play pickup sound if available
            if (audioSource != null && pickupSound != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }

            // Play particle effect if available
            if (pickupEffect != null)
            {
                // Instantiate the effect at the item's position and rotation
                Instantiate(pickupEffect, transform.position, transform.rotation);
                // Optionally, disable the item's visuals if the effect is prominent
                // For example, if you have a SpriteRenderer:
                // GetComponent<SpriteRenderer>().enabled = false;
                // GetComponent<Collider2D>().enabled = false; // Disable collider too
            }

            // Attempt to find the PlayerController to call its RestartGame method
            // This is good if PlayerController has specific pre-restart logic.
            PlayerController playerController = other.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // If the sound/effect needs time to play before scene reloads:
                // StartCoroutine(RestartAfterDelay(playerController, audioSource != null && pickupSound != null ? pickupSound.length : 0.1f));
                // Otherwise, restart immediately:
                playerController.RestartGame();
            }
            else
            {
                // Fallback: Directly reload the scene if PlayerController is not found
                // or if you prefer a direct restart.
                Debug.LogWarning("PlayerController script not found on the player. Restarting scene directly.");
                // If the sound/effect needs time to play:
                // StartCoroutine(RestartSceneDirectly(audioSource != null && pickupSound != null ? pickupSound.length : 0.1f));
                // Otherwise, restart immediately:
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            // If the item should disappear or be disabled after triggering:
            // gameObject.SetActive(false); // Option 1: Deactivate the item
            // Destroy(gameObject); // Option 2: Destroy the item (if sound/particles are parented or handled elsewhere)
            // If sound/particles are on this object and you want them to finish,
            // you might disable visuals/collider and destroy after a delay.
            // For instance, if you disabled renderer and collider:
            // Destroy(gameObject, pickupSound != null ? pickupSound.length : 0.5f);

        }
    }

    /*
    // Optional: Coroutine to delay restart if you have sound/effects
    private System.Collections.IEnumerator RestartAfterDelay(PlayerController pc, float delay)
    {
        // Disable visuals/collider so it looks collected
        var renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        yield return new WaitForSeconds(delay);
        pc.RestartGame();
    }

    private System.Collections.IEnumerator RestartSceneDirectly(float delay)
    {
        // Disable visuals/collider so it looks collected
        var renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    */


    // It's good practice to ensure the item has a Collider2D set to "Is Trigger".
    // You can add a check in Start or Awake if you want to enforce this.
    void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"HotDog on '{gameObject.name}' requires a Collider2D component.", this);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"Collider2D on '{gameObject.name}' (HotDog) is not set to 'Is Trigger'. It might not work as expected for item pickup.", this);
        }
    }
}
