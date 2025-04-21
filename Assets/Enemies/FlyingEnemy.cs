using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class FlyingEnemy : MonoBehaviour
{
    [Header("References")]
    public Transform enemyModel;
    public GameObject laserPrefab;
    public Transform enemyFirePoint;
    public GameObject explosionPrefab;

    // Direct references for drag-and-drop in Inspector
    [Tooltip("Drag the player transform here")]
    public Transform playerTransform;
    [Tooltip("Drag the cylinder level transform here")]
    public Transform cylinderTransform;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 90f;
    public float minDistanceToPlayer = 5f;
    public float detectionRange = 12f;
    public float heightMatchSpeed = 2f;

    [Header("Enemy Avoidance")]
    [Tooltip("Minimum distance to keep from other enemies")]
    public float minEnemyDistance = 3f;
    [Tooltip("How strongly enemies repel each other")]
    public float enemyRepulsionForce = 2f;
    [Tooltip("How far to check for other enemies")]
    public float enemyDetectionRange = 5f;
    [Tooltip("Layer mask for enemy detection")]
    public LayerMask enemyLayerMask;

    [Header("Random Movement")]
    [Tooltip("Enable random movement patterns")]
    public bool useRandomMovement = true;
    [Range(0f, 1f), Tooltip("How erratic the enemy's movement is (0-1)")]
    public float randomnessFactor = 0.4f;
    [Tooltip("How frequently direction changes occur (seconds)")]
    public float directionChangeFrequency = 2f;
    [Tooltip("Maximum vertical bobbing amount")]
    public float verticalBobbingAmount = 1.5f;
    [Tooltip("Speed of vertical bobbing")]
    public float verticalBobbingSpeed = 1.2f;
    [Tooltip("Random height variation from target height")]
    public float randomHeightVariation = 2f;
    [Tooltip("How quickly to change to a new random height")]
    public float heightChangeSpeed = 0.8f;
    [Tooltip("Chance to perform evasive maneuvers when shot (0-1)")]
    public float evasiveManeuverChance = 0.3f;

    [Header("Combat Settings")]
    public int maxHealth = 5;
    [Tooltip("How frequently to fire (shots per second)")]
    public float fireRate = 10f; // Increased significantly for constant firing
    public float fireRange = 10f;
    [Tooltip("Chance to fire downward instead of tangentially (0-1)")]
    public float downwardShotChance = 0.4f;
    [Tooltip("Always fire directly at player (overrides random direction)")]
    public bool alwaysAimAtPlayer = true;

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
    private float targetHeight;
    private float facingDirection = 1f; // 1 for forward tangent, -1 for reverse
    private Transform firePointTransform; // A separate transform just for aiming/firing

    // Random movement variables
    private float randomAngleOffset = 0f;
    private float nextDirectionChangeTime;
    private float randomHeightOffset = 0f;
    private float nextHeightChangeTime;
    private float currentBobbingPhase = 0f;
    private float evasiveMovementEndTime = 0f;
    private Vector3 evasiveDirection;
    private float originalRandomness;

    // Enemy avoidance variables
    private Vector3 avoidanceForce = Vector3.zero;
    private List<FlyingEnemy> nearbyEnemies = new List<FlyingEnemy>();
    private Collider[] enemyColliders = new Collider[10]; // Pre-allocate array for efficiency

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        currentHealth = maxHealth;
        originalRandomness = randomnessFactor;

        // Find references if not set through inspector
        if (cylinderTransform == null)
            cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Set default enemy layer mask if not set
        if (enemyLayerMask == 0)
            enemyLayerMask = LayerMask.GetMask("Default", "Enemy");

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

        // Create a separate transform for aiming
        firePointTransform = new GameObject("AimPoint").transform;
        firePointTransform.SetParent(transform);
        firePointTransform.localPosition = Vector3.zero;

        // Initialize random movement values
        nextDirectionChangeTime = Time.time + Random.Range(0.5f, directionChangeFrequency);
        nextHeightChangeTime = Time.time + Random.Range(0.5f, directionChangeFrequency);
        randomAngleOffset = Random.Range(-0.5f, 0.5f) * randomnessFactor;
        randomHeightOffset = Random.Range(-randomHeightVariation, randomHeightVariation);
        currentBobbingPhase = Random.Range(0f, 2f * Mathf.PI); // Random starting phase
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
                Debug.Log("Cylinder reference found by tag");
            }
            else
            {
                Debug.LogError("No object with 'Level' tag found for enemy! Please assign directly in inspector.");
            }
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("Player reference found by tag");
            }
            else
            {
                Debug.LogError("No object with 'Player' tag found for enemy! Please assign directly in inspector.");
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

        UpdateRandomMovementValues();

        // Detect nearby enemies and calculate avoidance force
        DetectNearbyEnemies();
        CalculateAvoidanceForce();

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

            // Set the aim point toward player
            if (alwaysAimAtPlayer)
            {
                // Make sure fire point is always looking at player
                Vector3 dirToPlayer = playerPos - currentPos;
                if (dirToPlayer.magnitude > 0.1f)
                {
                    firePointTransform.position = currentPos;
                    firePointTransform.LookAt(playerPos);
                }
            }
        }

        // Movement logic
        if (playerDetected)
        {
            // Check if we're in evasive maneuvers
            bool isEvading = Time.time < evasiveMovementEndTime;

            if (!isEvading)
            {
                // Stop at minimum distance
                if (distanceToPlayer > minDistanceToPlayer)
                {
                    // Move towards player with random offset
                    float moveDirection = Mathf.Sign(angleDiff);
                    float angularVelocity = moveSpeed * Time.fixedDeltaTime / cylinderRadius;

                    if (useRandomMovement)
                    {
                        angularVelocity += randomAngleOffset * angularVelocity;
                    }

                    // Apply avoidance influence to angular velocity (horizontal movement)
                    angularVelocity += avoidanceForce.x * Time.fixedDeltaTime;

                    currentAngle += moveDirection * angularVelocity;
                }
                else
                {
                    // At minimum distance, add slight random movement plus avoidance
                    float avoidanceAngularVelocity = avoidanceForce.x * Time.fixedDeltaTime;

                    if (useRandomMovement)
                    {
                        currentAngle += randomAngleOffset * moveSpeed * 0.5f * Time.fixedDeltaTime / cylinderRadius;
                    }

                    // Add avoidance movement even when not actively pursuing player
                    currentAngle += avoidanceAngularVelocity;
                }

                // Match player's height with smoothing plus random offset
                float playerHeight = Mathf.Clamp(playerTransform.position.y, minHeight, maxHeight);
                float targetPlayerHeight = playerHeight;

                if (useRandomMovement)
                {
                    // Add bobbing motion
                    float bobValue = Mathf.Sin(currentBobbingPhase) * verticalBobbingAmount;

                    // Add random height offset
                    targetPlayerHeight += randomHeightOffset + bobValue;
                }

                // Apply vertical avoidance influence
                targetPlayerHeight += avoidanceForce.y;

                targetHeight = Mathf.Lerp(targetHeight, targetPlayerHeight, Time.fixedDeltaTime * heightMatchSpeed);
            }
            else
            {
                // Apply evasive movement
                currentAngle += evasiveDirection.x * moveSpeed * 2f * Time.fixedDeltaTime / cylinderRadius;
                targetHeight = Mathf.Lerp(targetHeight, transform.position.y + evasiveDirection.y * 5f,
                                          Time.fixedDeltaTime * heightMatchSpeed * 2f);

                // Even when evading, apply some avoidance to prevent collision
                currentAngle += avoidanceForce.x * 0.5f * Time.fixedDeltaTime;
                targetHeight += avoidanceForce.y * 0.5f;
            }

            // Update position
            UpdatePositionAndRotation();

            // Constantly try to shoot when in range
            if (combinedDistance < fireRange && Time.time >= nextFireTime)
            {
                FireAtPlayer();
                nextFireTime = Time.time + (1f / fireRate); // Convert shots/sec to time interval
            }
        }
        else
        {
            // Patrol behavior when player not detected with random movements
            float patrolSpeed = moveSpeed * 0.5f;

            if (useRandomMovement)
            {
                // Apply random direction changes for patrolling
                currentAngle += (patrolSpeed + randomAngleOffset * patrolSpeed) * Time.fixedDeltaTime / cylinderRadius;

                // Random height changes while patrolling
                float patrolHeight = transform.position.y;

                // Add bobbing motion during patrol
                float bobValue = Mathf.Sin(currentBobbingPhase) * verticalBobbingAmount * 0.5f;

                // Gradually move toward random height offset during patrol
                targetHeight = Mathf.Lerp(targetHeight,
                                         Mathf.Clamp((minHeight + maxHeight) * 0.5f + randomHeightOffset + bobValue,
                                                   minHeight, maxHeight),
                                         Time.fixedDeltaTime * heightChangeSpeed);
            }

            // Apply avoidance even during patrol
            currentAngle += avoidanceForce.x * Time.fixedDeltaTime;
            targetHeight += avoidanceForce.y * 0.5f;

            UpdatePositionAndRotation();
        }

        // Normalize angle to prevent overflow
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Update bobbing phase
        currentBobbingPhase += verticalBobbingSpeed * Time.fixedDeltaTime;
        if (currentBobbingPhase > 2f * Mathf.PI)
        {
            currentBobbingPhase -= 2f * Mathf.PI;
        }
    }

    void DetectNearbyEnemies()
    {
        // Clear previous frame's data
        nearbyEnemies.Clear();

        // Non-allocating physics overlap to find nearby enemies
        int numColliders = Physics.OverlapSphereNonAlloc(
            transform.position,
            enemyDetectionRange,
            enemyColliders,
            enemyLayerMask
        );

        for (int i = 0; i < numColliders; i++)
        {
            // Skip if it's our own collider
            if (enemyColliders[i].gameObject == gameObject)
                continue;

            FlyingEnemy enemy = enemyColliders[i].GetComponent<FlyingEnemy>();
            if (enemy != null)
            {
                nearbyEnemies.Add(enemy);
            }
        }
    }

    void CalculateAvoidanceForce()
    {
        // Reset avoidance force
        avoidanceForce = Vector3.zero;

        if (nearbyEnemies.Count == 0)
            return;

        Vector3 currentCylinderPos = GetPositionOnCylinder(transform.position);

        foreach (FlyingEnemy enemy in nearbyEnemies)
        {
            // Skip if enemy is null (maybe destroyed)
            if (enemy == null) continue;

            // Get both positions on the cylinder surface
            Vector3 enemyCylinderPos = GetPositionOnCylinder(enemy.transform.position);

            // Calculate angular distance (for horizontal avoidance)
            float enemyAngle = Mathf.Atan2(
                enemyCylinderPos.x - cylinderTransform.position.x,
                enemyCylinderPos.z - cylinderTransform.position.z
            );
            float angularDist = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, enemyAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float horizontalDist = Mathf.Abs(angularDist * cylinderRadius);

            // Calculate vertical distance
            float verticalDist = Mathf.Abs(transform.position.y - enemy.transform.position.y);

            // Calculate combined distance (similar to detection calculations)
            float combinedDist = Mathf.Sqrt(horizontalDist * horizontalDist + verticalDist * verticalDist);

            // Only apply avoidance if enemy is too close
            if (combinedDist < minEnemyDistance)
            {
                // Calculate repulsion strength (stronger when closer)
                float repulsionStrength = enemyRepulsionForce * (1.0f - combinedDist / minEnemyDistance);

                // Apply horizontal repulsion (angular)
                float horizontalRepulsion = -Mathf.Sign(angularDist) * repulsionStrength;

                // Apply vertical repulsion
                float verticalRepulsion = Mathf.Sign(transform.position.y - enemy.transform.position.y) * repulsionStrength;

                // Accumulate forces
                avoidanceForce.x += horizontalRepulsion;
                avoidanceForce.y += verticalRepulsion;
            }
        }

        // Clamp maximum avoidance force to prevent extreme behaviors
        avoidanceForce.x = Mathf.Clamp(avoidanceForce.x, -2.0f, 2.0f);
        avoidanceForce.y = Mathf.Clamp(avoidanceForce.y, -2.0f, 2.0f);
    }

    Vector3 GetPositionOnCylinder(Vector3 worldPosition)
    {
        if (cylinderTransform == null) return worldPosition;

        // Project to cylinder surface
        Vector3 dirToCylinder = worldPosition - cylinderTransform.position;
        dirToCylinder.y = 0; // Flatten to get horizontal direction
        dirToCylinder = dirToCylinder.normalized * cylinderRadius;

        return new Vector3(
            cylinderTransform.position.x + dirToCylinder.x,
            worldPosition.y, // Keep the same Y
            cylinderTransform.position.z + dirToCylinder.z
        );
    }

    void UpdateRandomMovementValues()
    {
        if (!useRandomMovement) return;

        // Time to change direction?
        if (Time.time >= nextDirectionChangeTime)
        {
            randomAngleOffset = Random.Range(-1f, 1f) * randomnessFactor;
            nextDirectionChangeTime = Time.time + Random.Range(directionChangeFrequency * 0.5f,
                                                             directionChangeFrequency * 1.5f);
        }

        // Time to change height?
        if (Time.time >= nextHeightChangeTime)
        {
            randomHeightOffset = Random.Range(-randomHeightVariation, randomHeightVariation);
            nextHeightChangeTime = Time.time + Random.Range(directionChangeFrequency * 0.8f,
                                                         directionChangeFrequency * 2f);
        }
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

        // Calculate target rotation - add slight tilt based on vertical movement
        Quaternion baseRotation = Quaternion.LookRotation(facingDir, Vector3.up);

        if (useRandomMovement)
        {
            // Add a slight bank angle when moving up or down
            float verticalSpeed = (targetHeight - transform.position.y) * 10f;
            Quaternion bankRotation = Quaternion.Euler(Mathf.Clamp(verticalSpeed * -5f, -15f, 15f), 0, 0);
            Quaternion finalRotation = baseRotation * bankRotation;

            // Apply movement using physics
            rb.MovePosition(targetPosition);

            // Smoothly rotate to target rotation with banking
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, finalRotation, rotationSpeed * Time.deltaTime));

            // Apply additional model rotation if available
            if (enemyModel != null)
            {
                // Add a slight wobble to the model for more organic movement
                float wobbleX = Mathf.Sin(Time.time * 3.5f) * randomnessFactor * 5f;
                float wobbleZ = Mathf.Cos(Time.time * 2.7f) * randomnessFactor * 5f;
                enemyModel.localRotation = Quaternion.Euler(wobbleX, 0, wobbleZ);
            }
        }
        else
        {
            // Standard movement without random elements
            rb.MovePosition(targetPosition);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, baseRotation, rotationSpeed * Time.deltaTime));
        }
    }

    void FireAtPlayer()
    {
        if (enemyFirePoint == null || laserPrefab == null || playerTransform == null || cylinderTransform == null) return;

        Vector3 laserDir;
        Vector3 firePointPosition;

        if (alwaysAimAtPlayer)
        {
            // Use the dedicated aim point transform which is always looking at player
            firePointPosition = ProjectPointToCylinder(enemyFirePoint.position);
            laserDir = playerTransform.position - firePointPosition;
            laserDir.Normalize();
        }
        else
        {
            // Original logic for targeting
            // Get the tangent direction at our position
            Vector3 toCenter = transform.position - cylinderTransform.position;
            toCenter.y = 0;
            Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

            // Determine shot direction (tangential or downward)
            bool isTangentialShot = (Random.value > downwardShotChance);

            if (isTangentialShot)
            {
                // Standard tangential shot along cylinder surface
                laserDir = tangent * facingDirection;
            }
            else
            {
                // Calculate direction toward player combining both tangential and vertical components
                Vector3 enemyPos = transform.position;
                Vector3 playerPos = playerTransform.position;

                // Calculate vertical component
                float verticalComponent = playerPos.y - enemyPos.y;

                // Create a downward-angled shot combining tangential and downward directions
                float downRatio = Mathf.Clamp01(Mathf.Abs(verticalComponent) / 10f);

                // Combine tangential direction with downward direction
                Vector3 downDir = new Vector3(0, -1, 0);
                laserDir = Vector3.Lerp(tangent * facingDirection, downDir, downRatio).normalized;

                // If player is above, we might want to shoot up instead
                if (verticalComponent > 0)
                {
                    laserDir.y = Mathf.Abs(laserDir.y); // Make Y component positive
                }
            }

            // Make sure the fire point is positioned on the cylinder
            firePointPosition = ProjectPointToCylinder(enemyFirePoint.position);
        }

        // Create the laser with proper orientation
        GameObject laser = Instantiate(laserPrefab, firePointPosition, Quaternion.LookRotation(laserDir, Vector3.up));

        // Initialize laser with cylinder reference and direction
        EnemyLaser enemyLaser = laser.GetComponent<EnemyLaser>();
        if (enemyLaser != null)
        {
            // Use the same speed as player's laser, ensure proper initialization
            enemyLaser.Initialize(cylinderTransform, laserDir, 15f, 1, false);
        }
        else
        {
            // Basic projectile without EnemyLaser component
            Rigidbody laserRb = laser.GetComponent<Rigidbody>();
            if (laserRb != null)
            {
                laserRb.useGravity = false;
                laserRb.linearVelocity = laserDir * 15f;

                // Auto-destroy after 5 seconds if it doesn't hit anything
                Destroy(laser, 5f);
            }
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

        // Try to perform evasive maneuvers when hit
        if (useRandomMovement && Random.value < evasiveManeuverChance)
        {
            PerformEvasiveManeuvers();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void PerformEvasiveManeuvers()
    {
        // Set a random evasive direction
        evasiveDirection = new Vector3(
            Random.Range(-1f, 1f),  // Left or right
            Random.Range(-1f, 1f),  // Up or down
            0
        ).normalized;

        // Set duration for evasive movement
        evasiveMovementEndTime = Time.time + Random.Range(0.5f, 1.5f);

        // Temporarily increase randomness factor
        randomnessFactor = originalRandomness * 2f;

        // Schedule return to normal randomness
        Invoke("ResetRandomness", 1.5f);
    }

    void ResetRandomness()
    {
        randomnessFactor = originalRandomness;
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
        // Instantiate the explosion prefab at our position
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            // Fallback to creating particle effect if no prefab assigned
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
        }

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
            if (playerTransform == null)
                playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

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

        // Draw aim direction to player if enabled
        if (alwaysAimAtPlayer && playerTransform != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 toPlayer = playerTransform.position - cylinderPos;
            Gizmos.DrawRay(cylinderPos, toPlayer.normalized * 3f);
        }

        // Show detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cylinderPos, tangent * detectionRange * 0.5f);
        Gizmos.DrawRay(cylinderPos, -tangent * detectionRange * 0.5f);

        // Show fire range
        Gizmos.color = Color.red;
        Gizmos.DrawRay(cylinderPos, tangent * fireRange * 0.5f);
        Gizmos.DrawRay(cylinderPos, -tangent * fireRange * 0.5f);

        // Draw downward shot visualization
        if (playerTransform != null && !alwaysAimAtPlayer)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
            Vector3 downDirection = new Vector3(0, -1, 0);
            Vector3 combinedDirection = Vector3.Lerp(tangent * facingDirection, downDirection, 0.5f).normalized;
            Gizmos.DrawRay(cylinderPos, combinedDirection * 3f);
        }

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

        // Show random movement patterns
        if (useRandomMovement)
        {
            // Show random height variation
            Gizmos.color = new Color(0.5f, 0.8f, 0.5f, 0.3f); // Light green, transparent
            float randomHeightMax = cylinderPos.y + randomHeightVariation;
            float randomHeightMin = cylinderPos.y - randomHeightVariation;
            randomHeightMax = Mathf.Clamp(randomHeightMax, minHeight, maxHeight);
            randomHeightMin = Mathf.Clamp(randomHeightMin, minHeight, maxHeight);

            Vector3 randomHeightMaxPos = new Vector3(cylinderPos.x, randomHeightMax, cylinderPos.z);
            Vector3 randomHeightMinPos = new Vector3(cylinderPos.x, randomHeightMin, cylinderPos.z);
            Gizmos.DrawLine(randomHeightMinPos, randomHeightMaxPos);

            // Show bobbing range
            Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.3f); // Yellow, transparent
            float bobbingMax = cylinderPos.y + verticalBobbingAmount;
            float bobbingMin = cylinderPos.y - verticalBobbingAmount;
            bobbingMax = Mathf.Clamp(bobbingMax, minHeight, maxHeight);
            bobbingMin = Mathf.Clamp(bobbingMin, minHeight, maxHeight);

            Vector3 bobbingMaxPos = new Vector3(cylinderPos.x + 0.2f, bobbingMax, cylinderPos.z + 0.2f);
            Vector3 bobbingMinPos = new Vector3(cylinderPos.x + 0.2f, bobbingMin, cylinderPos.z + 0.2f);
            Gizmos.DrawLine(bobbingMinPos, bobbingMaxPos);
        }
    }
}