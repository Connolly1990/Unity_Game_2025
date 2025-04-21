using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class GroundEnemy : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag and drop the player object here")]
    public Transform player; // Player reference

    [Tooltip("Drag and drop the cylinder play space here")]
    public Transform cylinderTransform; // Cylinder reference

    public Transform bottomGuideline;
    public Transform enemyModel;
    public Transform firePoint; // Should be placed at the top of the enemy

    [Header("Basic Stats")]
    public int maxHealth = 2;
    public int currentHealth;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 90f;
    public float directionChangeTime = 3f;

    [Header("Combat Settings")]
    public float detectionRange = 10f;
    public float fireRate = 0.5f; // Faster fire rate for continuous shooting
    public GameObject laserPrefab;
    public float laserSpeed = 10f;
    public bool shootContinuously = true; // New property to enable continuous shooting

    [Header("Death Effect")]
    public GameObject explosionPrefab; // Add this line for the explosion prefab

    // Private variables
    private float currentAngle = 0f;
    private float cylinderRadius;
    private Rigidbody rb;
    private bool movingLeft = true;
    private float directionTimer;
    private Renderer[] enemyRenderers;
    private Color[] originalColors;
    private float fireTimer;
    private bool playerInRange = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        currentHealth = maxHealth;
        fireTimer = fireRate; // Initialize fire timer

        // Setup renderers for damage flash effect
        enemyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            originalColors[i] = enemyRenderers[i].material.color;
        }
    }

    void Start()
    {
        // Find references if not manually assigned
        if (cylinderTransform == null)
        {
            GameObject levelObject = GameObject.FindGameObjectWithTag("Level");
            if (levelObject != null)
            {
                cylinderTransform = levelObject.transform;
            }
            else
            {
                Debug.LogError("No cylinder play space assigned or found with 'Level' tag!");
                return;
            }
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("No player assigned or found with 'Player' tag!");
            }
        }

        // Find bottom guideline
        if (bottomGuideline == null)
        {
            GameObject guideObject = GameObject.FindGameObjectWithTag("FloorGuide");
            if (guideObject != null)
            {
                bottomGuideline = guideObject.transform;
            }
            else if (cylinderTransform != null)
            {
                bottomGuideline = cylinderTransform.Find("BottomGuideline");
                if (bottomGuideline == null)
                {
                    Debug.LogError("BottomGuideline not found!");
                    return;
                }
            }
        }

        cylinderRadius = cylinderTransform.localScale.x * 0.5f;
        currentAngle = Random.Range(0f, 2f * Mathf.PI);
        directionTimer = Random.Range(0f, directionChangeTime);
        ForcePositionToFloorGuideline();
    }

    void FixedUpdate()
    {
        if (cylinderTransform == null || bottomGuideline == null) return;

        // Handle movement
        directionTimer -= Time.fixedDeltaTime;
        if (directionTimer <= 0)
        {
            movingLeft = !movingLeft;
            directionTimer = directionChangeTime;
        }

        MoveAlongFloorGuideline();

        // Handle shooting
        if (player != null)
        {
            CheckPlayerInRange();

            fireTimer -= Time.fixedDeltaTime;
            if (fireTimer <= 0 && (shootContinuously || playerInRange))
            {
                ShootAtPlayer();
                fireTimer = fireRate;
            }
        }
    }

    void CheckPlayerInRange()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        playerInRange = distanceToPlayer <= detectionRange;
    }

    void ShootAtPlayer()
    {
        if (firePoint == null || laserPrefab == null) return;

        // Always shoot toward player's direction
        Vector3 shootDirection = (player.position - firePoint.position).normalized;

        // Create laser
        GameObject laser = Instantiate(laserPrefab, firePoint.position, Quaternion.identity);
        Rigidbody laserRb = laser.GetComponent<Rigidbody>();

        if (laserRb != null)
        {
            laserRb.linearVelocity = shootDirection * laserSpeed;
        }

        // Rotate laser to face shooting direction
        laser.transform.forward = shootDirection;
    }

    void ForcePositionToFloorGuideline()
    {
        float x = cylinderRadius * Mathf.Sin(currentAngle);
        float z = cylinderRadius * Mathf.Cos(currentAngle);
        float y = bottomGuideline.position.y;

        transform.position = new Vector3(x, y, z);

        Vector3 tangent = new Vector3(Mathf.Cos(currentAngle), 0f, -Mathf.Sin(currentAngle));
        if (movingLeft)
            tangent = -tangent;

        transform.forward = tangent;
    }

    void MoveAlongFloorGuideline()
    {
        if (movingLeft)
        {
            currentAngle += moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        }
        else
        {
            currentAngle -= moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        }

        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        float x = cylinderRadius * Mathf.Sin(currentAngle);
        float z = cylinderRadius * Mathf.Cos(currentAngle);
        float y = bottomGuideline.position.y;

        Vector3 newPosition = new Vector3(x, y, z);
        rb.MovePosition(newPosition);

        // Calculate tangent direction for rotation
        Vector3 tangent = new Vector3(Mathf.Cos(currentAngle), 0f, -Mathf.Sin(currentAngle));
        if (movingLeft)
            tangent = -tangent;

        // Keep the enemy looking forward along the cylinder while still being able to shoot up
        Quaternion targetRotation = Quaternion.LookRotation(tangent);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime));

        // Make sure firePoint still points toward the player when possible
        if (firePoint != null && player != null)
        {
            // Optional: Make firePoint subtly aim at player
            firePoint.LookAt(player);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            // Spawn explosion before destroying
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }

    private IEnumerator DamageFlash()
    {
        Color[] tempColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            tempColors[i] = enemyRenderers[i].material.color;
            enemyRenderers[i].material.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            if (enemyRenderers[i] != null)
            {
                enemyRenderers[i].material.color = tempColors[i];
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerLaser"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
        }
    }

    // Visualize detection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.forward * 2f);
        }
    }
}