using UnityEngine;
using System.Collections;

public class DodgeController : MonoBehaviour
{
    [Header("References")]
    public CylinderPlayerMovement playerMovement;
    public Transform playerModel;
    public ParticleSystem dodgeParticleEffect;
    public AudioSource audioSource;
    public AudioClip dodgeSoundEffect;

    [Header("Double Tap Settings")]
    public float doubleTapWindow = 0.3f;

    [Header("Dodge Settings")]
    public float dodgeDistance = 3f;
    public float dodgeDuration = 0.4f;
    public float dodgeCooldown = 1f;

    [Header("I-Frame Settings")]
    public float iFrameDuration = 0.6f;
    public float flashInterval = 0.1f;

    [Header("Visual Effects")]
    public float rollRotationSpeed = 720f; // Degrees per second for full rotation

    [Header("Bounds Settings")]
    public float boundsCheckRadius = 0.5f; // Radius for collision detection
    public LayerMask boundsLayerMask = -1; // Layer mask for bounds checking
    public int collisionCheckSteps = 10; // Number of points to check along dodge path

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Private variables
    private float[] lastKeyPressTimes = new float[4]; // W, A, S, D
    private bool isDodging = false;
    private bool isInvincible = false;
    private bool canDodge = true;
    private Vector3 dodgeStartPos;
    private Vector3 dodgeTargetPos;
    private Vector3 worldDodgeDirection;
    private bool isVerticalDodge; // NEW: Track if this is a vertical dodge
    private float dodgeStartTime;
    private float dodgeStartAngle;
    private float dodgeTargetAngle;
    private Renderer[] playerRenderers;
    private bool originalMovementEnabled;

    // Key indices for array
    private const int W_KEY = 0;
    private const int A_KEY = 1;
    private const int S_KEY = 2;
    private const int D_KEY = 3;

    void Start()
    {
        // Get references if not assigned
        if (playerMovement == null)
            playerMovement = GetComponent<CylinderPlayerMovement>();

        if (playerModel == null)
            playerModel = playerMovement.playerModel;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Get all renderers for flashing effect
        playerRenderers = GetComponentsInChildren<Renderer>();

        // Initialize key press times
        for (int i = 0; i < lastKeyPressTimes.Length; i++)
        {
            lastKeyPressTimes[i] = -doubleTapWindow - 1f;
        }
    }

    void Update()
    {
        if (!isDodging && canDodge)
        {
            HandleDoubleTapInput();
        }

        if (isDodging)
        {
            UpdateDodgeMovement();
        }
    }

    void HandleDoubleTapInput()
    {
        // Check for double taps
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            CheckDoubleTap(W_KEY, Vector3.forward); // Forward on cylinder (up)
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CheckDoubleTap(A_KEY, Vector3.left); // Left on cylinder
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            CheckDoubleTap(S_KEY, Vector3.back); // Backward on cylinder (down)
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            CheckDoubleTap(D_KEY, Vector3.right); // Right on cylinder
        }
    }

    void CheckDoubleTap(int keyIndex, Vector3 direction)
    {
        float currentTime = Time.time;
        float timeSinceLastPress = currentTime - lastKeyPressTimes[keyIndex];

        if (timeSinceLastPress <= doubleTapWindow)
        {
            // Double tap detected!
            InitiateDodge(direction);
        }

        lastKeyPressTimes[keyIndex] = currentTime;
    }

    void InitiateDodge(Vector3 localDirection)
    {
        if (!canDodge || isDodging) return;

        // Calculate dodge direction in world space relative to cylinder
        Vector3 playerPos = transform.position;
        Vector3 cylinderCenter = playerMovement.cylinderTransform.position;

        // Get the radial direction from cylinder center to player (only XZ plane)
        Vector3 radialDirection = new Vector3(
            playerPos.x - cylinderCenter.x,
            0,
            playerPos.z - cylinderCenter.z
        ).normalized;

        // Calculate tangent direction (perpendicular to radial, around the cylinder)
        Vector3 tangent = Vector3.Cross(Vector3.up, radialDirection).normalized;

        worldDodgeDirection = Vector3.zero;
        isVerticalDodge = false; // Reset flag

        // Convert local direction to world direction on cylinder
        if (localDirection == Vector3.forward) // Up
        {
            worldDodgeDirection = Vector3.up;
            isVerticalDodge = true;
        }
        else if (localDirection == Vector3.back) // Down
        {
            worldDodgeDirection = Vector3.down;
            isVerticalDodge = true;
        }
        else if (localDirection == Vector3.left) // Counter-clockwise around cylinder
        {
            worldDodgeDirection = tangent;
            isVerticalDodge = false;
        }
        else if (localDirection == Vector3.right) // Clockwise around cylinder
        {
            worldDodgeDirection = -tangent;
            isVerticalDodge = false;
        }

        if (showDebugInfo)
        {
            Debug.Log($"=== DODGE DEBUG ===");
            Debug.Log($"Local Direction: {localDirection}");
            Debug.Log($"World Dodge Direction: {worldDodgeDirection}");
            Debug.Log($"Is Vertical Dodge: {isVerticalDodge}");
            Debug.Log($"Radial Direction: {radialDirection}");
            Debug.Log($"Tangent: {tangent}");
        }

        // Store dodge start position
        dodgeStartPos = transform.position;

        // Calculate initial target position
        Vector3 tentativeTargetPos = CalculateTargetPosition(localDirection);

        // Check for bounds collision and adjust target if necessary
        Vector3 finalTargetPos = GetValidDodgeTarget(dodgeStartPos, tentativeTargetPos);

        // If the final target is too close to start (meaning we hit bounds immediately), cancel dodge
        if (Vector3.Distance(dodgeStartPos, finalTargetPos) < 0.5f)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Dodge cancelled - would immediately hit bounds. Direction: {localDirection}");
            }
            return;
        }

        // Set the valid target position
        dodgeTargetPos = finalTargetPos;

        isDodging = true;
        canDodge = false;
        originalMovementEnabled = playerMovement.enabled;

        // Disable player movement temporarily
        playerMovement.enabled = false;

        dodgeStartTime = Time.time;

        // Start visual and audio effects
        StartDodgeEffects();

        // Start I-frames
        StartCoroutine(IFrameCoroutine());

        // Start cooldown coroutine
        StartCoroutine(DodgeCooldownCoroutine());

        if (showDebugInfo)
        {
            Debug.Log($"=== TARGET CALCULATION ===");
            Debug.Log($"Is Vertical Dodge: {isVerticalDodge}");
            Debug.Log($"Start Position: {dodgeStartPos}");
            Debug.Log($"Tentative Target: {tentativeTargetPos}");
            Debug.Log($"Final Target: {finalTargetPos}");
        }
    }

    Vector3 CalculateTargetPosition(Vector3 localDirection)
    {
        if (isVerticalDodge) // Use the flag instead of checking worldDodgeDirection.y
        {
            Vector3 verticalTarget = dodgeStartPos + worldDodgeDirection * dodgeDistance;
            if (showDebugInfo)
            {
                Debug.Log($"Vertical Target Calculated: {verticalTarget}");
            }
            return verticalTarget;
        }
        else // Horizontal dodge around cylinder
        {
            float cylinderRadius = playerMovement.cylinderTransform.localScale.x * 0.5f;
            float currentAngle = Mathf.Atan2(transform.position.x - playerMovement.cylinderTransform.position.x,
                                           transform.position.z - playerMovement.cylinderTransform.position.z);

            dodgeStartAngle = currentAngle;
            float angleChange = dodgeDistance / cylinderRadius;
            if (localDirection == Vector3.right) angleChange = -angleChange;

            dodgeTargetAngle = currentAngle + angleChange;

            // Fixed: Use the player's current Y position directly, don't add it to cylinder position
            Vector3 horizontalTarget = new Vector3(
                playerMovement.cylinderTransform.position.x + cylinderRadius * Mathf.Sin(dodgeTargetAngle),
                transform.position.y, // Use player's current Y directly
                playerMovement.cylinderTransform.position.z + cylinderRadius * Mathf.Cos(dodgeTargetAngle)
            );

            if (showDebugInfo)
            {
                Debug.Log($"Horizontal Target Calculated: {horizontalTarget}");
                Debug.Log($"Current Y: {transform.position.y}, Target Y: {horizontalTarget.y}");
                Debug.Log($"Angle Change: {angleChange}, Start Angle: {dodgeStartAngle}, Target Angle: {dodgeTargetAngle}");
            }

            return horizontalTarget;
        }
    }

    Vector3 GetValidDodgeTarget(Vector3 startPos, Vector3 originalTarget)
    {
        Vector3 direction = (originalTarget - startPos).normalized;
        float maxDistance = Vector3.Distance(startPos, originalTarget);

        // Check multiple points along the path
        for (int i = 1; i <= collisionCheckSteps; i++)
        {
            float checkDistance = (maxDistance / collisionCheckSteps) * i;
            Vector3 checkPos = startPos + direction * checkDistance;

            // Add the arc height only for vertical dodges
            Vector3 checkPosWithArc = checkPos;
            if (isVerticalDodge) // Use the flag instead of checking worldDodgeDirection.y
            {
                float arcProgress = Mathf.Sin((float)i / collisionCheckSteps * Mathf.PI);
                checkPosWithArc.y += arcProgress * 0.5f; // Match the arc height from UpdateDodgeMovement
            }

            if (IsPositionBlocked(checkPosWithArc))
            {
                // Found a collision, return the previous safe position
                if (i == 1)
                {
                    // Even the first step is blocked
                    return startPos;
                }

                float safeDistance = (maxDistance / collisionCheckSteps) * (i - 1);
                Vector3 safePos = startPos + direction * safeDistance;

                if (showDebugInfo)
                {
                    Debug.Log($"Collision detected at step {i}, using safe position at distance {safeDistance}");
                }

                return safePos;
            }
        }

        // No collision found, use original target
        return originalTarget;
    }

    bool IsPositionBlocked(Vector3 position)
    {
        // Check for overlapping colliders at this position
        Collider[] overlapping = Physics.OverlapSphere(position, boundsCheckRadius, boundsLayerMask);

        foreach (Collider col in overlapping)
        {
            if (col.CompareTag("Ceiling") || col.CompareTag("Floor"))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Position {position} blocked by {col.tag}");
                }
                return true;
            }
        }

        return false;
    }

    void UpdateDodgeMovement()
    {
        float elapsedTime = Time.time - dodgeStartTime;
        float progress = elapsedTime / dodgeDuration;

        if (progress >= 1f)
        {
            // Dodge complete
            transform.position = dodgeTargetPos;
            isDodging = false;

            // Update the player movement's angle if we dodged horizontally
            if (!isVerticalDodge) // Use the flag instead of checking worldDodgeDirection.y
            {
                UpdatePlayerMovementAngle();
            }

            playerMovement.enabled = originalMovementEnabled;

            if (showDebugInfo)
            {
                Debug.Log("Dodge completed");
            }
            return;
        }

        // Smooth dodge movement using easing
        float easedProgress = EaseOutQuart(progress);
        Vector3 currentPos = Vector3.Lerp(dodgeStartPos, dodgeTargetPos, easedProgress);

        // Only add upward arc for vertical dodges, not horizontal ones
        if (isVerticalDodge) // Use the flag instead of checking worldDodgeDirection.y
        {
            float arcHeight = 0.5f;
            float arcProgress = Mathf.Sin(progress * Mathf.PI);
            currentPos.y += arcProgress * arcHeight;

            if (showDebugInfo && progress < 0.1f) // Only log at start
            {
                Debug.Log($"VERTICAL DODGE - Adding arc. Base Y: {Vector3.Lerp(dodgeStartPos, dodgeTargetPos, easedProgress).y}, Arc: {arcProgress * arcHeight}, Final Y: {currentPos.y}");
            }
        }
        else
        {
            if (showDebugInfo && progress < 0.1f) // Only log at start
            {
                Debug.Log($"HORIZONTAL DODGE - No arc. Y stays: {currentPos.y}");
            }
        }

        // Additional safety check during movement
        if (IsPositionBlocked(currentPos))
        {
            // If we somehow hit a collider during movement, stop the dodge
            transform.position = dodgeStartPos;
            isDodging = false;
            playerMovement.enabled = originalMovementEnabled;

            if (showDebugInfo)
            {
                Debug.Log("Dodge interrupted by collision during movement");
            }
            return;
        }

        transform.position = currentPos;
    }

    void StartDodgeEffects()
    {
        // Play particle effect
        if (dodgeParticleEffect != null)
        {
            dodgeParticleEffect.Play();
        }

        // Play sound effect
        if (audioSource != null && dodgeSoundEffect != null)
        {
            audioSource.PlayOneShot(dodgeSoundEffect);
        }

        // Start roll rotation
        StartCoroutine(RollRotationCoroutine());
    }

    IEnumerator RollRotationCoroutine()
    {
        if (playerModel == null) yield break;

        float rollDuration = dodgeDuration;
        float startTime = Time.time;

        while (Time.time - startTime < rollDuration)
        {
            float elapsed = Time.time - startTime;
            float rollProgress = elapsed / rollDuration;

            // Simple continuous spin around the Y-axis (up)
            float spinAngle = rollProgress * 360f;
            playerModel.Rotate(0, spinAngle * Time.deltaTime / rollDuration * 360f, 0, Space.Self);

            yield return null;
        }
    }

    IEnumerator IFrameCoroutine()
    {
        isInvincible = true;
        StartCoroutine(FlashEffect());

        yield return new WaitForSeconds(iFrameDuration);

        isInvincible = false;

        // Ensure renderers are visible
        SetRenderersVisible(true);

        if (showDebugInfo)
        {
            Debug.Log("I-frames ended");
        }
    }

    IEnumerator FlashEffect()
    {
        while (isInvincible)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(flashInterval);
            SetRenderersVisible(true);
            yield return new WaitForSeconds(flashInterval);
        }
    }

    IEnumerator DodgeCooldownCoroutine()
    {
        yield return new WaitForSeconds(dodgeCooldown);
        canDodge = true;

        if (showDebugInfo)
        {
            Debug.Log("Dodge cooldown complete");
        }
    }

    void SetRenderersVisible(bool visible)
    {
        foreach (Renderer renderer in playerRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    // Easing function for smooth dodge movement
    float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }

    // Public getters for other scripts
    public bool IsInvincible => isInvincible;
    public bool IsDodging => isDodging;
    public bool CanDodge => canDodge;
    public float IFrameTimeRemaining => isInvincible ? iFrameDuration - (Time.time - (Time.time - iFrameDuration)) : 0f;
    public float DodgeCooldown => dodgeCooldown;

    void UpdatePlayerMovementAngle()
    {
        // Update the player movement script's currentAngle to match our dodge position
        float cylinderRadius = playerMovement.cylinderTransform.localScale.x * 0.5f;
        float newAngle = Mathf.Atan2(transform.position.x - playerMovement.cylinderTransform.position.x,
                                   transform.position.z - playerMovement.cylinderTransform.position.z);
        playerMovement.SetCurrentAngle(newAngle);
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Draw dodge target position during dodge
        if (isDodging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(dodgeTargetPos, 0.3f);
            Gizmos.DrawLine(dodgeStartPos, dodgeTargetPos);

            // Draw collision check points along the path
            Vector3 direction = (dodgeTargetPos - dodgeStartPos).normalized;
            float distance = Vector3.Distance(dodgeStartPos, dodgeTargetPos);

            for (int i = 1; i <= collisionCheckSteps; i++)
            {
                float checkDistance = (distance / collisionCheckSteps) * i;
                Vector3 checkPos = dodgeStartPos + direction * checkDistance;

                // Add arc height only for vertical dodges
                if (isVerticalDodge) // Use the flag instead of checking worldDodgeDirection.y
                {
                    float arcProgress = Mathf.Sin((float)i / collisionCheckSteps * Mathf.PI);
                    checkPos.y += arcProgress * 0.5f;
                }

                Gizmos.color = IsPositionBlocked(checkPos) ? Color.red : Color.green;
                Gizmos.DrawWireSphere(checkPos, boundsCheckRadius * 0.5f);
            }
        }

        // Draw invincibility status
        if (isInvincible)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }

        // Draw bounds check radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, boundsCheckRadius);
    }
}