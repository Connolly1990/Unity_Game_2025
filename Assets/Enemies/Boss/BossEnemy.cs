using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BossEnemy : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag and drop the player object here")]
    public Transform player;

    [Tooltip("Drag and drop the cylinder play space here")]
    public Transform cylinderTransform;

    public Transform middleGuideline;
    public Transform enemyModel;

    [Header("Audio References")]
    public AudioClip explosionSound;
    public AudioClip damageSound;

    [Header("Basic Stats")]
    public int maxHealth = 20;
    public int currentHealth;
    public bool isInvulnerable = false;
    public float invulnerabilityTime = 0.5f;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 45f;

    [Header("Death Effect")]
    public GameObject explosionPrefab;
    public int explosionCount = 5;
    public float explosionInterval = 0.2f;

    // Private variables
    private float currentAngle = 0f;
    private float cylinderRadius;
    private Rigidbody rb;
    private Renderer[] enemyRenderers;
    private Color[] originalColors;
    private AudioSource audioSource;
    private bool isDying = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        currentHealth = maxHealth;

        // Setup renderers for damage flash effect
        enemyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            originalColors[i] = enemyRenderers[i].material.color;
        }

        // Setup audio source with 2D configuration
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio source for 2D sound
        audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D
        audioSource.playOnAwake = false;
        audioSource.loop = false;
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

        // Find middle guideline
        if (middleGuideline == null)
        {
            GameObject guideObject = GameObject.FindGameObjectWithTag("MiddleGuide");
            if (guideObject != null)
            {
                middleGuideline = guideObject.transform;
            }
            else if (cylinderTransform != null)
            {
                middleGuideline = cylinderTransform.Find("MiddleGuideline");
                if (middleGuideline == null)
                {
                    Debug.LogError("MiddleGuideline not found!");
                    return;
                }
            }
        }

        cylinderRadius = cylinderTransform.localScale.x * 0.5f;
        currentAngle = Random.Range(0f, 2f * Mathf.PI);
        ForcePositionToMiddleGuideline();

        // Initialize combat scripts
        BossLaserCombat laserCombat = GetComponent<BossLaserCombat>();
        if (laserCombat != null)
        {
            laserCombat.Initialize(player, cylinderTransform);
        }

        BossMissileCombat missileCombat = GetComponent<BossMissileCombat>();
        if (missileCombat != null)
        {
            missileCombat.Initialize(player, cylinderTransform);
        }
    }

    void FixedUpdate()
    {
        if (cylinderTransform == null || middleGuideline == null || isDying) return;

        MoveAlongMiddleGuideline();
        FacePlayer();
    }

    void ForcePositionToMiddleGuideline()
    {
        float x = cylinderRadius * Mathf.Sin(currentAngle);
        float z = cylinderRadius * Mathf.Cos(currentAngle);
        float y = middleGuideline.position.y;

        transform.position = new Vector3(x, y, z);
    }

    void MoveAlongMiddleGuideline()
    {
        // Constant movement in one direction
        currentAngle += moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        float x = cylinderRadius * Mathf.Sin(currentAngle);
        float z = cylinderRadius * Mathf.Cos(currentAngle);
        float y = middleGuideline.position.y;

        Vector3 newPosition = new Vector3(x, y, z);
        rb.MovePosition(newPosition);
    }

    void FacePlayer()
    {
        if (player != null)
        {
            Vector3 dirToPlayer = player.position - transform.position;
            // Ensure we're only rotating in the XZ plane
            dirToPlayer.y = 0;

            if (dirToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(dirToPlayer);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, rotationSpeed * Time.deltaTime));
            }
        }
        else
        {
            // Face the center of the cylinder if no player
            Vector3 dirToCenter = new Vector3(0, transform.position.y, 0) - transform.position;
            dirToCenter.y = 0;

            if (dirToCenter != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(dirToCenter);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, rotationSpeed * Time.deltaTime));
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable || isDying) return;

        currentHealth -= damage;

        // Play damage sound
        if (damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        StartCoroutine(DamageFlash());
        StartCoroutine(BriefInvulnerability());

        if (currentHealth <= 0 && !isDying)
        {
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator BriefInvulnerability()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }

    IEnumerator DeathSequence()
    {
        isDying = true;

        // Disable all combat scripts
        MonoBehaviour[] combatScripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in combatScripts)
        {
            if (script != this && script.enabled)
            {
                script.enabled = false;
            }
        }

        // Multiple explosion effects
        for (int i = 0; i < explosionCount; i++)
        {
            // Random position offset within boss bounds
            Vector3 explosionPos = transform.position + new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            );

            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, explosionPos, Quaternion.identity);
            }

            // Play explosion sound
            if (explosionSound != null)
            {
                audioSource.PlayOneShot(explosionSound);
            }

            yield return new WaitForSeconds(explosionInterval);
        }

        // Final explosion
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private IEnumerator DamageFlash()
    {
        // Store original colors
        Color[] tempColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            tempColors[i] = enemyRenderers[i].material.color;
            enemyRenderers[i].material.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        // Restore original colors
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
                playerHealth.TakeDamage(2); // Boss deals more damage on collision
            }
        }
    }

    // Visualize detection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}