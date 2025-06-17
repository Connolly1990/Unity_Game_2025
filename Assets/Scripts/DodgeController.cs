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

        isDodging = true;
        canDodge = false;
        originalMovementEnabled = playerMovement.enabled;

        // Disable player movement temporarily
        playerMovement.enabled = false;

        // Calculate dodge direction in world space relative to cylinder
        Vector3 toCenter = playerMovement.cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        Vector3 normal = -toCenter.normalized;

        worldDodgeDirection = Vector3.zero;

        // Convert local direction to world direction on cylinder
        if (localDirection == Vector3.forward) // Up
        {
            worldDodgeDirection = Vector3.up;
        }
        else if (localDirection == Vector3.back) // Down
        {
            worldDodgeDirection = Vector3.down;
        }
        else if (localDirection == Vector3.left) // Counter-clockwise around cylinder
        {
            worldDodgeDirection = tangent;
        }
        else if (localDirection == Vector3.right) // Clockwise around cylinder
        {
            worldDodgeDirection = -tangent;
        }

        // Store dodge start and target positions
        dodgeStartPos = transform.position;
        dodgeStartTime = Time.time;

        // Calculate target position
        if (worldDodgeDirection.y != 0) // Vertical dodge
        {
            dodgeTargetPos = dodgeStartPos + worldDodgeDirection * dodgeDistance;
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

            dodgeTargetPos = new Vector3(
                cylinderRadius * Mathf.Sin(dodgeTargetAngle),
                transform.position.y,
                cylinderRadius * Mathf.Cos(dodgeTargetAngle)
            );
        }

        // Start visual and audio effects
        StartDodgeEffects();

        // Start I-frames
        StartCoroutine(IFrameCoroutine());

        // Start cooldown coroutine
        StartCoroutine(DodgeCooldownCoroutine());

        if (showDebugInfo)
        {
            Debug.Log($"Dodge initiated in direction: {localDirection}, World direction: {worldDodgeDirection}");
        }
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
            if (worldDodgeDirection.y == 0) // Horizontal dodge
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
        transform.position = currentPos;

        // Add slight upward arc for visual appeal
        float arcHeight = 0.5f;
        float arcProgress = Mathf.Sin(progress * Mathf.PI);
        currentPos.y += arcProgress * arcHeight;
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
        }

        // Draw invincibility status
        if (isInvincible)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}