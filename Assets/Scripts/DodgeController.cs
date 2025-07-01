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
    public float rollRotationSpeed = 720f;

    [Header("Bounds Settings")]
    public float boundsCheckRadius = 0.5f;
    public LayerMask boundsLayerMask = -1;
    public int collisionCheckSteps = 10;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private float[] lastKeyPressTimes = new float[4]; // W, A, S, D
    private bool isDodging = false;
    private bool isInvincible = false;
    private bool canDodge = true;
    private Vector3 dodgeStartPos;
    private Vector3 dodgeTargetPos;
    private Vector3 worldDodgeDirection;
    private bool isVerticalDodge;
    private float dodgeStartTime;
    private float dodgeStartAngle;
    private float dodgeTargetAngle;
    private Renderer[] playerRenderers;
    private bool originalMovementEnabled;

    private const int W_KEY = 0;
    private const int A_KEY = 1;
    private const int S_KEY = 2;
    private const int D_KEY = 3;

    void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<CylinderPlayerMovement>();

        if (playerModel == null)
            playerModel = playerMovement.playerModel;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        playerRenderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < lastKeyPressTimes.Length; i++)
            lastKeyPressTimes[i] = -doubleTapWindow - 1f;
    }

    void Update()
    {
        if (!isDodging && canDodge)
            HandleDoubleTapInput();

        if (isDodging)
            UpdateDodgeMovement();
    }

    void HandleDoubleTapInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            CheckDoubleTap(W_KEY, Vector3.forward);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            CheckDoubleTap(A_KEY, Vector3.left);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            CheckDoubleTap(S_KEY, Vector3.back);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            CheckDoubleTap(D_KEY, Vector3.right);
    }

    void CheckDoubleTap(int keyIndex, Vector3 direction)
    {
        float currentTime = Time.time;
        float timeSinceLastPress = currentTime - lastKeyPressTimes[keyIndex];

        if (timeSinceLastPress <= doubleTapWindow)
            InitiateDodge(direction);

        lastKeyPressTimes[keyIndex] = currentTime;
    }

    void InitiateDodge(Vector3 localDirection)
    {
        if (!canDodge || isDodging) return;

        Vector3 playerPos = transform.position;
        Vector3 cylinderCenter = playerMovement.cylinderTransform.position;

        Vector3 radialDirection = new Vector3(
            playerPos.x - cylinderCenter.x,
            0,
            playerPos.z - cylinderCenter.z
        ).normalized;

        Vector3 tangent = Vector3.Cross(Vector3.up, radialDirection).normalized;

        worldDodgeDirection = Vector3.zero;
        isVerticalDodge = false;

        if (localDirection == Vector3.forward)
        {
            worldDodgeDirection = Vector3.up;
            isVerticalDodge = true;
        }
        else if (localDirection == Vector3.back)
        {
            worldDodgeDirection = Vector3.down;
            isVerticalDodge = true;
        }
        else if (localDirection == Vector3.left)
        {
            worldDodgeDirection = tangent;
            isVerticalDodge = false;
        }
        else if (localDirection == Vector3.right)
        {
            worldDodgeDirection = -tangent;
            isVerticalDodge = false;
        }

        dodgeStartPos = transform.position;
        Vector3 tentativeTargetPos = CalculateTargetPosition(localDirection);
        Vector3 finalTargetPos = GetValidDodgeTarget(dodgeStartPos, tentativeTargetPos);

        if (Vector3.Distance(dodgeStartPos, finalTargetPos) < 0.5f)
            return;

        dodgeTargetPos = finalTargetPos;
        isDodging = true;
        canDodge = false;
        originalMovementEnabled = playerMovement.enabled;
        playerMovement.enabled = false;
        dodgeStartTime = Time.time;

        StartDodgeEffects();
        StartCoroutine(IFrameCoroutine());
        StartCoroutine(DodgeCooldownCoroutine());
    }

    Vector3 CalculateTargetPosition(Vector3 localDirection)
    {
        if (isVerticalDodge)
            return dodgeStartPos + worldDodgeDirection * dodgeDistance;

        float cylinderRadius = playerMovement.cylinderTransform.localScale.x * 0.5f;
        float currentAngle = Mathf.Atan2(transform.position.x - playerMovement.cylinderTransform.position.x,
                                         transform.position.z - playerMovement.cylinderTransform.position.z);

        dodgeStartAngle = currentAngle;
        float angleChange = dodgeDistance / cylinderRadius;
        if (localDirection == Vector3.right) angleChange = -angleChange;

        dodgeTargetAngle = currentAngle + angleChange;

        return new Vector3(
            playerMovement.cylinderTransform.position.x + cylinderRadius * Mathf.Sin(dodgeTargetAngle),
            transform.position.y,
            playerMovement.cylinderTransform.position.z + cylinderRadius * Mathf.Cos(dodgeTargetAngle)
        );
    }

    Vector3 GetValidDodgeTarget(Vector3 startPos, Vector3 originalTarget)
    {
        Vector3 direction = (originalTarget - startPos).normalized;
        float maxDistance = Vector3.Distance(startPos, originalTarget);

        for (int i = 1; i <= collisionCheckSteps; i++)
        {
            float checkDistance = (maxDistance / collisionCheckSteps) * i;
            Vector3 checkPos = startPos + direction * checkDistance;
            Vector3 checkPosWithArc = checkPos;

            if (isVerticalDodge)
                checkPosWithArc.y += Mathf.Sin((float)i / collisionCheckSteps * Mathf.PI) * 0.5f;

            if (IsPositionBlocked(checkPosWithArc))
            {
                if (i == 1)
                    return startPos;

                float safeDistance = (maxDistance / collisionCheckSteps) * (i - 1);
                return startPos + direction * safeDistance;
            }
        }

        return originalTarget;
    }

    bool IsPositionBlocked(Vector3 position)
    {
        Collider[] overlapping = Physics.OverlapSphere(position, boundsCheckRadius, boundsLayerMask);
        foreach (Collider col in overlapping)
        {
            if (col.CompareTag("Ceiling") || col.CompareTag("Floor"))
                return true;
        }
        return false;
    }

    void UpdateDodgeMovement()
    {
        float elapsedTime = Time.time - dodgeStartTime;
        float progress = elapsedTime / dodgeDuration;

        if (progress >= 1f)
        {
            transform.position = dodgeTargetPos;
            isDodging = false;
            if (!isVerticalDodge)
                UpdatePlayerMovementAngle();

            playerMovement.enabled = originalMovementEnabled;
            return;
        }

        float easedProgress = EaseOutQuart(progress);
        Vector3 currentPos = Vector3.Lerp(dodgeStartPos, dodgeTargetPos, easedProgress);

        if (isVerticalDodge)
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.5f;

        if (IsPositionBlocked(currentPos))
        {
            transform.position = dodgeStartPos;
            isDodging = false;
            playerMovement.enabled = originalMovementEnabled;
            return;
        }

        transform.position = currentPos;
    }

    // ✅ THIS IS THE ONLY MODIFIED METHOD
    void StartDodgeEffects()
    {
        if (dodgeParticleEffect != null)
        {
            ParticleSystem smoke = Instantiate(dodgeParticleEffect, transform.position, Quaternion.identity);
            Destroy(smoke.gameObject, smoke.main.duration + smoke.main.startLifetime.constantMax);
        }

        if (audioSource != null && dodgeSoundEffect != null)
            audioSource.PlayOneShot(dodgeSoundEffect);

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
        SetRenderersVisible(true);
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
    }

    void SetRenderersVisible(bool visible)
    {
        foreach (Renderer renderer in playerRenderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }

    public bool IsInvincible => isInvincible;
    public bool IsDodging => isDodging;
    public bool CanDodge => canDodge;
    public float IFrameTimeRemaining => isInvincible ? iFrameDuration - (Time.time - (Time.time - iFrameDuration)) : 0f;
    public float DodgeCooldown => dodgeCooldown;

    void UpdatePlayerMovementAngle()
    {
        float cylinderRadius = playerMovement.cylinderTransform.localScale.x * 0.5f;
        float newAngle = Mathf.Atan2(transform.position.x - playerMovement.cylinderTransform.position.x,
                                     transform.position.z - playerMovement.cylinderTransform.position.z);
        playerMovement.SetCurrentAngle(newAngle);
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        if (isDodging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(dodgeTargetPos, 0.3f);
            Gizmos.DrawLine(dodgeStartPos, dodgeTargetPos);

            Vector3 direction = (dodgeTargetPos - dodgeStartPos).normalized;
            float distance = Vector3.Distance(dodgeStartPos, dodgeTargetPos);

            for (int i = 1; i <= collisionCheckSteps; i++)
            {
                float checkDistance = (distance / collisionCheckSteps) * i;
                Vector3 checkPos = dodgeStartPos + direction * checkDistance;

                if (isVerticalDodge)
                    checkPos.y += Mathf.Sin((float)i / collisionCheckSteps * Mathf.PI) * 0.5f;

                Gizmos.color = IsPositionBlocked(checkPos) ? Color.red : Color.green;
                Gizmos.DrawWireSphere(checkPos, boundsCheckRadius * 0.5f);
            }
        }

        if (isInvincible)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, boundsCheckRadius);
    }
}
