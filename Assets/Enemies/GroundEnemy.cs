using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class GroundEnemyMovement : MonoBehaviour
{
    [Header("References")]
    public Transform enemyModel;
    public Transform cylinderTransform;
    public Transform bottomGuideline;

    [Header("Basic Stats")]
    public int maxHealth = 2;
    public int currentHealth;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 90f;
    public float directionChangeTime = 3f;

    // Private variables
    private float currentAngle = 0f;
    private float cylinderRadius;
    private Rigidbody rb;
    private bool movingLeft = true;
    private float directionTimer;
    private Renderer[] enemyRenderers;
    private Color[] originalColors;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Initialize health
        currentHealth = maxHealth;

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
                Debug.LogError("No object with 'Level' tag found for ground enemy!");
                return;
            }
        }

        // Find bottom guideline - critical for this enemy type
        if (bottomGuideline == null)
        {
            // Try to find by tag first
            GameObject guideObject = GameObject.FindGameObjectWithTag("FloorGuide");
            if (guideObject != null)
            {
                bottomGuideline = guideObject.transform;
            }
            // Try to find as a child of cylinder with specific name
            else if (cylinderTransform != null)
            {
                bottomGuideline = cylinderTransform.Find("BottomGuideline");
                if (bottomGuideline == null)
                {
                    Debug.LogError("BottomGuideline not found! Ground enemy needs this to function.");
                    return;
                }
            }
            else
            {
                Debug.LogError("No floor guide found! Ground enemy cannot function.");
                return;
            }
        }

        // Get cylinder radius
        cylinderRadius = cylinderTransform.localScale.x * 0.5f;

        // Randomize starting angle
        currentAngle = Random.Range(0f, 2f * Mathf.PI);

        // Initialize random direction change timer
        directionTimer = Random.Range(0f, directionChangeTime);

        // Force position to bottom guideline immediately
        ForcePositionToFloorGuideline();
    }

    void FixedUpdate()
    {
        if (cylinderTransform == null || bottomGuideline == null) return;

        // Handle direction changes
        directionTimer -= Time.fixedDeltaTime;
        if (directionTimer <= 0)
        {
            // Change direction
            movingLeft = !movingLeft;
            directionTimer = directionChangeTime;
        }

        // Move along cylinder
        MoveAlongFloorGuideline();
    }

    void ForcePositionToFloorGuideline()
    {
        // Calculate position on cylinder at the FLOOR level
        float x = cylinderRadius * Mathf.Sin(currentAngle);
        float z = cylinderRadius * Mathf.Cos(currentAngle);
        float y = bottomGuideline.position.y; // Critical - use the floor guideline's height

        // Set position immediately
        transform.position = new Vector3(x, y, z);

        // Set rotation to face tangent to cylinder
        Vector3 tangent = new Vector3(Mathf.Cos(currentAngle), 0f, -Mathf.Sin(currentAngle));
        if (movingLeft)
            tangent = -tangent;

        transform.forward = tangent;
    }

    void MoveAlongFloorGuideline()
    {
        // Update angle based on direction
        if (movingLeft)
        {
            currentAngle += moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        }
        else
        {
            currentAngle -= moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        }

        // Keep angle within 0-2π range
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Calculate new position - ALWAYS on floor
        float x = cylinderRadius * Mathf.Sin(currentAngle);
        float z = cylinderRadius * Mathf.Cos(currentAngle);
        float y = bottomGuideline.position.y; // Critical - floor height

        Vector3 newPosition = new Vector3(x, y, z);
        rb.MovePosition(newPosition);

        // Calculate facing direction (tangent to cylinder)
        Vector3 tangent = new Vector3(Mathf.Cos(currentAngle), 0f, -Mathf.Sin(currentAngle));
        if (movingLeft)
            tangent = -tangent;

        // Smooth rotation
        Quaternion targetRotation = Quaternion.LookRotation(tangent);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime));
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Flash effect
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            // Destroy enemy when health reaches zero
            Destroy(gameObject);
        }
    }

    private IEnumerator DamageFlash()
    {
        // Store original colors and set to red
        Color[] tempColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            tempColors[i] = enemyRenderers[i].material.color;
            enemyRenderers[i].material.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        // Restore colors
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
        // Check if hit by player laser
        if (other.CompareTag("PlayerLaser"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Optional: Handle collisions with player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Deal damage to player or other logic
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (cylinderTransform == null)
        {
            GameObject levelObject = GameObject.FindGameObjectWithTag("Level");
            if (levelObject != null)
            {
                cylinderTransform = levelObject.transform;
                cylinderRadius = cylinderTransform.localScale.x * 0.5f;
            }
            else
            {
                return;
            }
        }

        if (bottomGuideline != null)
        {
            // Draw floor guideline - this is where enemy should be
            Gizmos.color = Color.yellow;
            float guideY = bottomGuideline.position.y;

            // Draw a circle showing the floor path
            int segments = 32;
            Vector3 prevPoint = cylinderTransform.position + new Vector3(
                cylinderRadius * Mathf.Sin(0),
                guideY,
                cylinderRadius * Mathf.Cos(0)
            );

            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * 2f * Mathf.PI;
                Vector3 nextPoint = cylinderTransform.position + new Vector3(
                    cylinderRadius * Mathf.Sin(angle),
                    guideY,
                    cylinderRadius * Mathf.Cos(angle)
                );
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            // Draw current position on guideline
            Vector3 currentPos = cylinderTransform.position + new Vector3(
                cylinderRadius * Mathf.Sin(currentAngle),
                guideY,
                cylinderRadius * Mathf.Cos(currentAngle)
            );

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentPos, 0.3f);

            // Draw movement direction
            Vector3 tangent = new Vector3(Mathf.Cos(currentAngle), 0f, -Mathf.Sin(currentAngle));
            if (movingLeft)
                tangent = -tangent;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(currentPos, tangent * 2f);
        }
    }
}