using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    public float parallaxEffect = 0.5f;
    
    [Header("Vertical Behavior")]
    public bool followCameraY = false;
    [Range(0f, 1f)]
    public float verticalParallaxEffect = 0f;
    
    [Header("Infinite Scrolling")]
    public bool infiniteHorizontal = true;
    public bool infiniteVertical = false;

    private float startPosX;
    private float startPosY;
    private float spriteWidth;
    private float spriteHeight;
    private Transform cam;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // Store initial position
        startPosX = transform.position.x;
        startPosY = transform.position.y;
        
        // Get sprite bounds
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteWidth = spriteRenderer.bounds.size.x;
            spriteHeight = spriteRenderer.bounds.size.y;
        }
        
        // Get camera reference
        cam = Camera.main.transform;
        
        if (cam == null)
        {
            Debug.LogError("Main Camera not found! Make sure your camera has the 'MainCamera' tag.");
        }
    }

    void Update()
    {
        if (cam == null) return;

        // Calculate horizontal parallax
        float relativePosX = cam.position.x * (1 - parallaxEffect);
        float distanceX = cam.position.x * parallaxEffect;
        
        // Calculate vertical parallax (only if enabled)
        float newY = startPosY;
        if (followCameraY)
        {
            float distanceY = cam.position.y * verticalParallaxEffect;
            newY = startPosY + distanceY;
        }

        // Apply new position
        transform.position = new Vector3(startPosX + distanceX, newY, transform.position.z);

        // Handle infinite horizontal scrolling
        if (infiniteHorizontal && spriteRenderer != null)
        {
            if (relativePosX > startPosX + spriteWidth)
            {
                startPosX += spriteWidth;
            }
            else if (relativePosX < startPosX - spriteWidth)
            {
                startPosX -= spriteWidth;
            }
        }

        // Handle infinite vertical scrolling (if enabled)
        if (infiniteVertical && followCameraY && spriteRenderer != null)
        {
            float relativePosY = cam.position.y * (1 - verticalParallaxEffect);
            if (relativePosY > startPosY + spriteHeight)
            {
                startPosY += spriteHeight;
            }
            else if (relativePosY < startPosY - spriteHeight)
            {
                startPosY -= spriteHeight;
            }
        }
    }

    // Helper method to reset parallax position
    [ContextMenu("Reset Parallax Position")]
    public void ResetPosition()
    {
        startPosX = transform.position.x;
        startPosY = transform.position.y;
    }
}