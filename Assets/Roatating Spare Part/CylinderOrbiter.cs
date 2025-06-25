using UnityEngine;

public enum RotationMode
{
    KeepOriginal,           // Don't change rotation at all
    FaceMovementDirection,  // Face the direction of orbital movement
    FaceCenter,            // Face toward the center
    FaceAwayFromCenter,    // Face away from the center
    Custom                 // Use custom rotation settings
}

[System.Serializable]
public class OrbitSettings
{
    [Header("Orbit Behavior")]
    [Tooltip("Speed of orbital movement (positive = clockwise, negative = counter-clockwise)")]
    public float orbitSpeed = 30f;

    [Tooltip("Should the object orbit automatically?")]
    public bool autoOrbit = true;

    [Tooltip("Should the object face the direction of movement?")]
    public bool faceMovementDirection = false;  // Changed default to false

    [Tooltip("Should the object face toward or away from the center?")]
    public bool faceCenter = false;

    [Header("Vertical Movement")]
    [Tooltip("Should the object move up and down?")]
    public bool enableVerticalMovement = false;

    [Tooltip("Speed of vertical oscillation")]
    public float verticalSpeed = 2f;

    [Tooltip("Height range of vertical movement")]
    public float verticalRange = 3f;

    [Tooltip("Starting phase of vertical movement (0-1)")]
    [Range(0f, 1f)]
    public float verticalPhase = 0f;

    [Header("Distance Variation")]
    [Tooltip("Should the orbit distance change over time?")]
    public bool enableDistanceVariation = false;

    [Tooltip("Speed of distance variation")]
    public float distanceVariationSpeed = 1f;

    [Tooltip("Amount of distance variation")]
    public float distanceVariationAmount = 2f;
}

public class CylinderOrbiter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The cylinder transform to orbit around")]
    public Transform cylinderTransform;

    [Tooltip("Reference to the player movement script (optional - for synchronization)")]
    public CylinderPlayerMovement playerMovement;

    [Header("Orbit Configuration")]
    [Tooltip("Distance from the cylinder center")]
    public float orbitRadius = 10f;

    [Tooltip("Starting height on the cylinder")]
    public float startingHeight = 0f;

    [Tooltip("Starting angle in degrees (0 = forward, 90 = right, etc.)")]
    [Range(0f, 360f)]
    public float startingAngle = 0f;

    [Header("Orbit Settings")]
    public OrbitSettings orbitSettings = new OrbitSettings();

    [Header("Rotation Control")]
    [Tooltip("How should the object's rotation be handled?")]
    public RotationMode rotationMode = RotationMode.KeepOriginal;

    [Tooltip("Should the object continue its own rotation while orbiting?")]
    public bool allowSelfRotation = true;

    [Tooltip("Self rotation speed (degrees per second)")]
    public Vector3 selfRotationSpeed = Vector3.zero;

    [Header("Boundary Settings")]
    [Tooltip("Minimum height limit")]
    public float minHeight = -5f;

    [Tooltip("Maximum height limit")]
    public float maxHeight = 20f;

    [Tooltip("Should respect cylinder boundaries?")]
    public bool respectBoundaries = true;

    [Header("Debug")]
    [Tooltip("Show debug information in gizmos")]
    public bool showDebugGizmos = true;

    [Tooltip("Color for debug gizmos")]
    public Color gizmoColor = Color.cyan;

    // Private variables
    private float currentAngle;
    private float currentRadius;
    private float baseHeight;
    private float timeOffset;
    private bool isInitialized = false;
    private Vector3 lastPosition;
    private Quaternion originalRotation;
    private Quaternion currentSelfRotation;

    // Public properties for external access
    public float CurrentAngle => currentAngle * Mathf.Rad2Deg;
    public float CurrentRadius => currentRadius;
    public Vector3 CenterPosition => cylinderTransform != null ? cylinderTransform.position : Vector3.zero;
    public bool IsMoving => Vector3.Distance(transform.position, lastPosition) > 0.01f;

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (!isInitialized) return;

        UpdateOrbit();
        UpdatePosition();
        UpdateRotation();

        lastPosition = transform.position;
    }

    public void Initialize()
    {
        // Find cylinder if not assigned
        if (cylinderTransform == null)
        {
            GameObject cylinder = GameObject.FindGameObjectWithTag("Cylinder");
            if (cylinder != null)
                cylinderTransform = cylinder.transform;
            else
                Debug.LogWarning($"CylinderOrbiter on {gameObject.name}: No cylinder transform assigned and none found with 'Cylinder' tag!");
        }

        if (cylinderTransform == null) return;

        // Calculate cylinder radius if using player movement reference
        if (playerMovement != null)
        {
            float cylinderScale = cylinderTransform.localScale.x;
            float playerRadius = cylinderScale * 0.5f;
            if (orbitRadius == 10f) // Default value, adjust to match player
                orbitRadius = playerRadius + 2f; // Slightly outside player orbit
        }

        // Set initial values
        currentAngle = startingAngle * Mathf.Deg2Rad;
        currentRadius = orbitRadius;
        baseHeight = startingHeight;
        originalRotation = transform.rotation;
        currentSelfRotation = Quaternion.identity;

        // Random time offset for variation between multiple orbiters
        timeOffset = Random.Range(0f, 2f * Mathf.PI);

        // Set initial position
        UpdatePosition();

        isInitialized = true;

        Debug.Log($"CylinderOrbiter initialized on {gameObject.name}");
    }

    void UpdateOrbit()
    {
        if (!isInitialized || cylinderTransform == null) return;

        // Update angle for orbital movement
        if (orbitSettings.autoOrbit)
        {
            float angleSpeed = orbitSettings.orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;
            currentAngle += angleSpeed / currentRadius; // Adjust speed based on radius

            // Keep angle in 0-2π range
            currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);
        }

        // Update radius with distance variation
        if (orbitSettings.enableDistanceVariation)
        {
            float distanceOffset = Mathf.Sin(Time.time * orbitSettings.distanceVariationSpeed + timeOffset)
                                 * orbitSettings.distanceVariationAmount;
            currentRadius = orbitRadius + distanceOffset;
        }
        else
        {
            currentRadius = orbitRadius;
        }
    }

    void UpdatePosition()
    {
        if (!isInitialized || cylinderTransform == null) return;

        // Calculate base position on cylinder
        Vector3 cylinderCenter = cylinderTransform.position;
        Vector3 orbitPosition = new Vector3(
            cylinderCenter.x + currentRadius * Mathf.Sin(currentAngle),
            0f,
            cylinderCenter.z + currentRadius * Mathf.Cos(currentAngle)
        );

        // Calculate height
        float targetHeight = baseHeight;

        if (orbitSettings.enableVerticalMovement)
        {
            float verticalOffset = Mathf.Sin((Time.time * orbitSettings.verticalSpeed) +
                                           (orbitSettings.verticalPhase * 2f * Mathf.PI) + timeOffset)
                                 * orbitSettings.verticalRange;
            targetHeight += verticalOffset;
        }

        // Apply height boundaries
        if (respectBoundaries)
        {
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
        }

        orbitPosition.y = cylinderCenter.y + targetHeight;

        // Apply position
        transform.position = orbitPosition;
    }

    void UpdateRotation()
    {
        if (!isInitialized || cylinderTransform == null) return;

        // Handle self rotation first
        if (allowSelfRotation && selfRotationSpeed != Vector3.zero)
        {
            Vector3 rotationDelta = selfRotationSpeed * Time.deltaTime;
            currentSelfRotation *= Quaternion.Euler(rotationDelta);
        }

        Quaternion targetRotation = originalRotation * currentSelfRotation;

        // Apply orbital rotation based on mode
        switch (rotationMode)
        {
            case RotationMode.KeepOriginal:
                // Just use original rotation + self rotation
                break;

            case RotationMode.FaceMovementDirection:
                if (orbitSettings.autoOrbit && Mathf.Abs(orbitSettings.orbitSpeed) > 0.01f)
                {
                    Vector3 toCenter = cylinderTransform.position - transform.position;
                    toCenter.y = 0;
                    Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
                    Vector3 movementDirection = tangent * Mathf.Sign(orbitSettings.orbitSpeed);

                    if (movementDirection.magnitude > 0.01f)
                    {
                        targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up) * currentSelfRotation;
                    }
                }
                break;

            case RotationMode.FaceCenter:
                {
                    Vector3 toCenter = cylinderTransform.position - transform.position;
                    toCenter.y = 0;
                    if (toCenter.magnitude > 0.01f)
                    {
                        targetRotation = Quaternion.LookRotation(toCenter.normalized, Vector3.up) * currentSelfRotation;
                    }
                }
                break;

            case RotationMode.FaceAwayFromCenter:
                {
                    Vector3 awayFromCenter = transform.position - cylinderTransform.position;
                    awayFromCenter.y = 0;
                    if (awayFromCenter.magnitude > 0.01f)
                    {
                        targetRotation = Quaternion.LookRotation(awayFromCenter.normalized, Vector3.up) * currentSelfRotation;
                    }
                }
                break;

            case RotationMode.Custom:
                {
                    // Use the old orbit settings for backward compatibility
                    if (orbitSettings.faceMovementDirection)
                    {
                        Vector3 toCenterCustom = cylinderTransform.position - transform.position;
                        toCenterCustom.y = 0;
                        Vector3 tangentCustom = Vector3.Cross(toCenterCustom.normalized, Vector3.up);
                        Vector3 movementDirectionCustom = tangentCustom * Mathf.Sign(orbitSettings.orbitSpeed);

                        if (movementDirectionCustom.magnitude > 0.01f)
                        {
                            targetRotation = Quaternion.LookRotation(movementDirectionCustom, Vector3.up) * currentSelfRotation;
                        }
                    }
                    else if (orbitSettings.faceCenter)
                    {
                        Vector3 toCenterCustom2 = cylinderTransform.position - transform.position;
                        toCenterCustom2.y = 0;
                        if (toCenterCustom2.magnitude > 0.01f)
                        {
                            targetRotation = Quaternion.LookRotation(toCenterCustom2.normalized, Vector3.up) * currentSelfRotation;
                        }
                    }
                }
                break;
        }

        transform.rotation = targetRotation;
    }

    // Public methods for external control
    public void SetOrbitSpeed(float speed)
    {
        orbitSettings.orbitSpeed = speed;
    }

    public void SetOrbitRadius(float radius)
    {
        orbitRadius = radius;
        currentRadius = radius;
    }

    public void SetHeight(float height)
    {
        baseHeight = height;
    }

    public void SetAngle(float angleDegrees)
    {
        currentAngle = angleDegrees * Mathf.Deg2Rad;
    }

    public void SetRotationMode(RotationMode mode)
    {
        rotationMode = mode;
    }

    public void SetSelfRotationSpeed(Vector3 rotationSpeed)
    {
        selfRotationSpeed = rotationSpeed;
    }

    public void ToggleAutoOrbit()
    {
        orbitSettings.autoOrbit = !orbitSettings.autoOrbit;
    }

    public void ToggleSelfRotation()
    {
        allowSelfRotation = !allowSelfRotation;
    }

    public void SynchronizeWithPlayer()
    {
        if (playerMovement != null)
        {
            // Match player's angle with optional offset
            currentAngle = Mathf.Atan2(playerMovement.transform.position.x - cylinderTransform.position.x,
                                     playerMovement.transform.position.z - cylinderTransform.position.z);
            currentAngle += startingAngle * Mathf.Deg2Rad; // Apply offset
        }
    }

    // Reset to starting position
    public void ResetToStart()
    {
        if (cylinderTransform != null)
        {
            currentAngle = startingAngle * Mathf.Deg2Rad;
            currentRadius = orbitRadius;
            baseHeight = startingHeight;
            currentSelfRotation = Quaternion.identity;
            UpdatePosition();
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || cylinderTransform == null) return;

        Gizmos.color = gizmoColor;

        // Draw orbit path
        Vector3 center = cylinderTransform.position;
        float drawRadius = isInitialized ? currentRadius : orbitRadius;

        // Draw orbit circle
        int segments = 64;
        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 point = new Vector3(
                center.x + drawRadius * Mathf.Sin(angle),
                center.y + (isInitialized ? transform.position.y : center.y + startingHeight),
                center.z + drawRadius * Mathf.Cos(angle)
            );

            if (i > 0)
                Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        // Draw current position indicator
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw direction indicator
        if (rotationMode == RotationMode.FaceMovementDirection && isInitialized)
        {
            Vector3 toCenter = cylinderTransform.position - transform.position;
            toCenter.y = 0;
            Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
            Vector3 direction = tangent * Mathf.Sign(orbitSettings.orbitSpeed);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }

        // Draw connection to center
        Gizmos.color = gizmoColor * 0.5f;
        Vector3 centerAtHeight = new Vector3(center.x, transform.position.y, center.z);
        Gizmos.DrawLine(transform.position, centerAtHeight);
    }

    void OnValidate()
    {
        // Clamp values in editor
        orbitRadius = Mathf.Max(0.1f, orbitRadius);
        startingAngle = Mathf.Repeat(startingAngle, 360f);

        if (Application.isPlaying && isInitialized)
        {
            currentRadius = orbitRadius;
            UpdatePosition();
        }
    }
}