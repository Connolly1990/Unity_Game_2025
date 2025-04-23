using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CylinderPlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cylinderTransform;
    public Transform playerModel;
    public Transform missileSpawnPoint; // Reference to the missile spawn point

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;
    public float boundaryOffset = 0.5f;

    [Header("Missile Settings")]
    public GameObject missilePrefab; // Drag your missile prefab here
    public float missileCooldown = 2f; // Time between missile shots
    public AudioClip missileFireSound; // Optional sound effect

    public bool CanShoot { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;
    public int CurrentDirection { get; private set; } = 0; // 0=idle, 1=forward, -1=backward

    private float currentAngle = 0f;
    private float cylinderRadius;
    private Rigidbody rb;
    private int horizontalDirection = 0;
    private int verticalDirection = 0;
    private bool canMoveUp = true;
    private bool canMoveDown = true;
    private bool hasMovedSinceStart = false;

    // Missile firing variables
    private float nextFireTime = 0f;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        cylinderRadius = cylinderTransform.localScale.x * 0.5f;
        currentAngle = Mathf.Atan2(transform.position.x - cylinderTransform.position.x,
                                 transform.position.z - cylinderTransform.position.z);
        UpdatePositionAndRotation(transform.position.y, true);
        CanShoot = false;

        // Get or add AudioSource component for missile sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && missileFireSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Validate missile spawn point
        if (missileSpawnPoint == null)
        {
            Debug.LogWarning("Missile spawn point not assigned. Using player position instead.");
            missileSpawnPoint = transform;
        }
    }

    void Update()
    {
        HandleInput();
        HandleMissileFiring();
    }

    void FixedUpdate()
    {
        IsMoving = horizontalDirection != 0 || verticalDirection != 0;

        if (IsMoving && !hasMovedSinceStart)
        {
            hasMovedSinceStart = true;
            CanShoot = true;
        }

        // Update raw direction based on input
        int rawDirection = (int)Mathf.Sign(horizontalDirection);

        // Update position using angle (normalized to prevent overflow)
        currentAngle += horizontalDirection * moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Calculate actual direction relative to cylinder tangent
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        // This will ensure CurrentDirection is relative to the player's orientation on the cylinder
        if (rawDirection != 0)
            CurrentDirection = rawDirection;

        // Vertical movement
        float newY = rb.position.y;
        if (verticalDirection > 0 && canMoveUp)
            newY += moveSpeed * Time.fixedDeltaTime;
        else if (verticalDirection < 0 && canMoveDown)
            newY -= moveSpeed * Time.fixedDeltaTime;

        UpdatePositionAndRotation(newY);
        UpdateShipOrientation();
    }

    void HandleInput()
    {
        // Horizontal input (toggle-based)
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            horizontalDirection = horizontalDirection == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            horizontalDirection = horizontalDirection == -1 ? 0 : -1;

        // Vertical input (toggle-based)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            verticalDirection = verticalDirection == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            verticalDirection = verticalDirection == -1 ? 0 : -1;
    }

    void HandleMissileFiring()
    {
        // Check if player can shoot and F key is pressed
        if (CanShoot && Input.GetKeyDown(KeyCode.F) && Time.time >= nextFireTime)
        {
            FireMissile();
        }
    }

    void FireMissile()
    {
        if (missilePrefab == null)
        {
            Debug.LogError("Missile prefab not assigned!");
            return;
        }

        // Set the cooldown for next shot
        nextFireTime = Time.time + missileCooldown;

        // Get the forward direction considering player orientation on cylinder
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        Vector3 launchDirection = tangent * CurrentDirection;

        // Instantiate missile at spawn point
        GameObject missile = Instantiate(missilePrefab, missileSpawnPoint.position, Quaternion.identity);

        // Initialize the missile
        HomingMissile homingMissile = missile.GetComponent<HomingMissile>();
        if (homingMissile != null)
        {
            homingMissile.Initialize(launchDirection);
        }

        // Play sound effect if available
        if (audioSource != null && missileFireSound != null)
        {
            audioSource.PlayOneShot(missileFireSound);
        }
    }

    void UpdatePositionAndRotation(float newY, bool snap = false)
    {
        Vector3 targetPosition = new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            newY,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        Vector3 toCenter = cylinderTransform.position - targetPosition;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(tangent * CurrentDirection, Vector3.up)
                                 * Quaternion.Euler(270, 0, 0);

        if (snap)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            rb.MovePosition(targetPosition);
            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation,
                           rotationSpeed * Time.fixedDeltaTime));
        }
    }

    void UpdateShipOrientation()
    {
        if (CurrentDirection == 0) return;

        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        playerModel.rotation = Quaternion.LookRotation(tangent * CurrentDirection, Vector3.up)
                             * Quaternion.Euler(270, 0, 0);
    }

    void OnDrawGizmos()
    {
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, tangent * CurrentDirection * 2f);

        // Draw missile spawn point
        if (missileSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(missileSpawnPoint.position, 0.2f);
        }
    }
}