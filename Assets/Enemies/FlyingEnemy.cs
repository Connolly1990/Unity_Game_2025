using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyingEnemy : MonoBehaviour
{
    [Header("References")]
    public Transform enemyModel;
    public GameObject laserPrefab;
    public Transform enemyFirePoint;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 90f;
    public float minDistanceToPlayer = 5f;
    public float detectionRange = 12f;
    public float heightMatchSpeed = 2f;

    [Header("Combat Settings")]
    public int maxHealth = 5;
    public float fireRate = 1.5f;
    public float fireRange = 10f;

    [Header("Cylinder Boundaries")]
    public float maxHeight = 10f;
    public float minHeight = 0f;

    // Private variables
    private float currentAngle = 0f;
    private float cylinderRadius;
    private Rigidbody rb;
    private float nextFireTime;
    private int currentHealth;
    private bool playerDetected = false;
    private Transform cylinderTransform;
    private Transform playerTransform;
    private float targetHeight;
    private float facingDirection = 1f; // 1 for forward tangent, -1 for reverse

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        currentHealth = maxHealth;

        // Find references immediately in Awake
        cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (cylinderTransform != null)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;
            // Calculate initial angle based on current position
            Vector3 toEnemy = transform.position - cylinderTransform.position;
            currentAngle = Mathf.Atan2(toEnemy.x, toEnemy.z);
        }

        // Set initial target height to current height
        targetHeight = transform.position.y;

        // Create firepoint if missing
        if (enemyFirePoint == null)
        {
            enemyFirePoint = new GameObject("FirePoint").transform;
            enemyFirePoint.SetParent(transform);
            enemyFirePoint.localPosition = new Vector3(0, 0.5f, 1f);
        }
    }

    void Start()
    {
        // Double check references if somehow missed in Awake
        if (cylinderTransform == null)
        {
            GameObject levelObject = GameObject.FindGameObjectWithTag("Level");
            if (levelObject != null)
            {
                cylinderTransform = levelObject.transform;
            }
            else
            {
                Debug.LogError("No object with 'Level' tag found for enemy!");
            }
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("No object with 'Player' tag found for enemy!");
            }
        }

        // Initialize cylinder properties
        if (cylinderTransform != null)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;

            // If we're too far from the cylinder, project onto it
            float distanceToCenter = Vector3.Distance(
                new Vector3(transform.position.x, cylinderTransform.position.y, transform.position.z),
                new Vector3(cylinderTransform.position.x, cylinderTransform.position.y, cylinderTransform.position.z)
            );

            if (Mathf.Abs(distanceToCenter - cylinderRadius) > 0.5f)
            {
                // We're not on the cylinder surface, fix position
                AlignToCylinder();
            }
        }
    }

    void AlignToCylinder()
    {
        if (cylinderTransform == null) return;

        // Calculate the correct position on the cylinder surface
        Vector3 correctPosition = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            targetHeight,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        // Calculate facing direction (tangent to cylinder)
        Vector3 toCenter = cylinderTransform.position - correctPosition;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        // Apply facing direction
        Vector3 finalFacingDir = tangent * facingDirection;

        // Set position and rotation
        transform.position = correctPosition;
        transform.rotation = Quaternion.LookRotation(finalFacingDir, Vector3.up);

        // Apply model rotation if needed
        if (enemyModel != null)
        {
            // Reset local rotation first
            enemyModel.localRotation = Quaternion.identity;
        }
    }

    void FixedUpdate()
    {
        if (playerTransform == null || cylinderTransform == null) return;

        // Calculate player angle on cylinder
        float playerAngle = Mathf.Atan2(
            playerTransform.position.x - cylinderTransform.position.x,
            playerTransform.position.z - cylinderTransform.position.z
        );

        // Calculate angular distance to player (shortest path)
        float angleDiff = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, playerAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        // Calculate actual distance to player along cylinder
        float distanceToPlayer = Mathf.Abs(angleDiff * cylinderRadius);

        // Calculate vertical distance
        float verticalDist = Mathf.Abs(transform.position.y - playerTransform.position.y);

        // Combine distances for full detection
        float combinedDistance = Mathf.Sqrt(distanceToPlayer * distanceToPlayer + verticalDist * verticalDist);

        // Check if player is within detection range
        playerDetected = combinedDistance < detectionRange;

        // Determine facing direction based on player position 
        if (playerDetected)
        {
            // Create vectors for current position and player position on cylinder
            Vector3 currentPos = cylinderTransform.position + new Vector3(
                cylinderRadius * Mathf.Sin(currentAngle),
                transform.position.y,
                cylinderRadius * Mathf.Cos(currentAngle)
            );

            Vector3 playerPos = cylinderTransform.position + new Vector3(
                cylinderRadius * Mathf.Sin(playerAngle),
                playerTransform.position.y,
                cylinderRadius * Mathf.Cos(playerAngle)
            );

            // Get tangent at our position
            Vector3 toCenter = cylinderTransform.position - currentPos;
            toCenter.y = 0;
            Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

            // Calculate relative position to determine which way to face
            Vector3 toPlayer = playerPos - currentPos;
            float dot = Vector3.Dot(tangent, toPlayer);

            // Set facing direction based on where player is
            facingDirection = (dot >= 0) ? 1f : -1f;
        }

        // Movement logic
        if (playerDetected)
        {
            // Stop at minimum distance
            if (distanceToPlayer > minDistanceToPlayer)
            {
                // Move towards player
                float moveDirection = Mathf.Sign(angleDiff);
                currentAngle += moveDirection * moveSpeed * Time.fixedDeltaTime / cylinderRadius;
            }

            // Match player's height with smoothing (FIXED: no more automatic upwards drift)
            float playerHeight = Mathf.Clamp(playerTransform.position.y, minHeight, maxHeight);
            targetHeight = Mathf.Lerp(targetHeight, playerHeight, Time.fixedDeltaTime * heightMatchSpeed);

            // Update position
            UpdatePositionAndRotation();

            // Try to shoot if in range
            if (combinedDistance < fireRange && Time.time >= nextFireTime)
            {
                FireAtPlayer();
                nextFireTime = Time.time + fireRate;
            }
        }
        else
        {
            // Patrol behavior when player not detected
            currentAngle += moveSpeed * 0.5f * Time.fixedDeltaTime / cylinderRadius;

            // FIXED: Keep current height when patrolling, don't change it
            // This prevents unwanted vertical drifting

            UpdatePositionAndRotation();
        }

        // Normalize angle to prevent overflow
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);
    }

    void UpdatePositionAndRotation()
    {
        if (cylinderTransform == null) return;

        // Clamp height to cylinder bounds
        targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);

        // Calculate new position on cylinder surface
        Vector3 targetPosition = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            targetHeight,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        // Calculate tangent direction at this position
        Vector3 toCenter = cylinderTransform.position - targetPosition;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        // Apply facing direction to tangent
        Vector3 facingDir = tangent * facingDirection;

        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(facingDir, Vector3.up);

        // Apply movement using physics
        rb.MovePosition(targetPosition);

        // Smoothly rotate to target rotation
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime));
    }

    void FireAtPlayer()
    {
        if (enemyFirePoint == null || laserPrefab == null || playerTransform == null || cylinderTransform == null) return;

        // Get the tangent direction at our position
        Vector3 toCenter = transform.position - cylinderTransform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        // Fire in the direction we're facing
        Vector3 laserDir = tangent * facingDirection;

        // Make sure the fire point is positioned on the cylinder
        Vector3 firePointPosition = ProjectPointToCylinder(enemyFirePoint.position);

        // Create the laser with proper orientation
        GameObject laser = Instantiate(laserPrefab, firePointPosition, Quaternion.LookRotation(laserDir, Vector3.up));

        // Initialize laser with cylinder reference and direction
        EnemyLaser enemyLaser = laser.GetComponent<EnemyLaser>();
        if (enemyLaser != null)
        {
            // Use the same speed as player's laser, ensure proper initialization
            enemyLaser.Initialize(cylinderTransform, laserDir, 15f, 1, false);
        }
    }
    // Helper to project any point onto the cylinder surface (keeping Y)
    Vector3 ProjectPointToCylinder(Vector3 point)
    {
        if (cylinderTransform == null) return point;

        // Calculate angle on cylinder
        Vector3 relativePos = point - cylinderTransform.position;
        float angle = Mathf.Atan2(relativePos.x, relativePos.z);

        // Project to cylinder surface at same height
        return new Vector3(
            cylinderTransform.position.x + cylinderRadius * Mathf.Sin(angle),
            point.y,
            cylinderTransform.position.z + cylinderRadius * Mathf.Cos(angle)
        );
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Optional: Add damage flash effect
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        // Get renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color[] originalColors = new Color[renderers.Length];

        // Store original colors and set to red
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
            renderers[i].material.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }

    void Die()
    {
        // Create death effect
        GameObject deathEffect = new GameObject("DeathEffect");
        deathEffect.transform.position = transform.position;

        // Add particle system
        ParticleSystem ps = deathEffect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = Color.red;
        main.startSize = 0.5f;
        main.startLifetime = 1f;
        main.startSpeed = 2f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        ps.Play();
        Destroy(deathEffect, 2f);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if hit by player laser
        if (other.CompareTag("PlayerLaser"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (cylinderTransform == null)
        {
            // Try to find in editor time
            GameObject levelObject = GameObject.FindGameObjectWithTag("Level");
            if (levelObject != null)
            {
                cylinderTransform = levelObject.transform;
            }
            else
            {
                return;
            }
        }

        // Calculate cylinder radius if not set
        if (cylinderRadius <= 0)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;
        }

        // Calculate position on cylinder
        Vector3 cylinderPos = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            transform.position.y,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        // Calculate tangent
        Vector3 toCenter = cylinderTransform.position - cylinderPos;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        // Draw our position on cylinder
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(cylinderPos, 0.2f);
        Gizmos.DrawLine(transform.position, cylinderPos);

        // Draw our facing direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(cylinderPos, tangent * facingDirection * 2f);

        // Show detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cylinderPos, tangent * detectionRange * 0.5f);
        Gizmos.DrawRay(cylinderPos, -tangent * detectionRange * 0.5f);

        // Show fire range
        Gizmos.color = Color.red;
        Gizmos.DrawRay(cylinderPos, tangent * fireRange * 0.5f);
        Gizmos.DrawRay(cylinderPos, -tangent * fireRange * 0.5f);

        // Draw cylinder alignment point
        if (enemyFirePoint != null)
        {
            Gizmos.color = Color.green;
            Vector3 projectedFirePoint = ProjectPointToCylinder(enemyFirePoint.position);
            Gizmos.DrawLine(enemyFirePoint.position, projectedFirePoint);
            Gizmos.DrawWireSphere(projectedFirePoint, 0.2f);
        }

        // Show height constraints
        Gizmos.color = Color.cyan;
        Vector3 minPos = new Vector3(cylinderPos.x, minHeight, cylinderPos.z);
        Vector3 maxPos = new Vector3(cylinderPos.x, maxHeight, cylinderPos.z);
        Gizmos.DrawLine(minPos, maxPos);
    }
}