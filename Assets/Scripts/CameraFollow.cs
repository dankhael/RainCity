using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target; // jogador
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Smoothing (Optional)")]
    public bool useSmoothing = false;
    public float smoothSpeed = 2f;
    
    [Header("Bounds (Optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;
        
        // Apply bounds if enabled
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }

        // Apply smoothing if enabled
        if (useSmoothing)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / smoothSpeed);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    // Helper method to get camera's starting X position for parallax
    public float GetStartingXPosition()
    {
        return transform.position.x - (target != null ? target.position.x : 0);
    }

    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
            Gizmos.DrawWireCube(center, size);
        }
    }
}