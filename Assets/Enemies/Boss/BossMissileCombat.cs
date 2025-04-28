using UnityEngine;
using System.Collections;

public class BossMissileCombat : MonoBehaviour
{
    [Header("References")]
    public Transform missileFirePoint;
    public GameObject missilePrefab;

    [Header("Missile Attack Settings")]
    public float missileFireRate = 5f;
    public int missileDamage = 2;
    public float missileSpeed = 10f;
    public float maxFiringAngle = 60f; // Maximum angle to still fire at player
    public AudioClip missileLaunchSound;

    [Header("Advanced Settings")]
    public bool fireMissilesInSequence = true;
    public int missilesPerSequence = 3;
    public float sequenceDelay = 0.5f;
    public float missileCooldown = 8f; // Longer cooldown after firing sequence

    // Private variables
    private Transform player;
    private Transform cylinderTransform;
    private float cylinderRadius;
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private bool isInitialized = false;
    private bool isFiringSequence = false;
    private BossEnemy bossController;

    void Awake()
    {
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.playOnAwake = false;
        }

        // Get reference to the boss controller
        bossController = GetComponent<BossEnemy>();
    }

    public void Initialize(Transform playerTransform, Transform cylinderRef)
    {
        player = playerTransform;
        cylinderTransform = cylinderRef;

        if (cylinderTransform != null)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;
        }

        isInitialized = true;
        nextFireTime = Time.time + Random.Range(2f, missileFireRate);
    }

    void Update()
    {
        if (!isInitialized || player == null || isFiringSequence ||
            bossController == null || bossController.isInvulnerable) return;

        // Check if it's time to fire
        if (Time.time >= nextFireTime)
        {
            // Check if boss is facing player within a reasonable angle
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            if (angle <= maxFiringAngle)
            {
                if (fireMissilesInSequence)
                {
                    StartCoroutine(FireMissileSequence());
                }
                else
                {
                    FireMissile();
                    nextFireTime = Time.time + missileFireRate;
                }
            }
        }
    }

    IEnumerator FireMissileSequence()
    {
        isFiringSequence = true;

        for (int i = 0; i < missilesPerSequence; i++)
        {
            FireMissile();
            yield return new WaitForSeconds(sequenceDelay);
        }

        nextFireTime = Time.time + missileCooldown;
        isFiringSequence = false;
    }

    void FireMissile()
    {
        if (missilePrefab == null || missileFirePoint == null || player == null) return;

        // Calculate direction to player with slight prediction
        Vector3 playerPos = player.position;
        Vector3 playerVelocity = Vector3.zero;

        // Try to get player velocity if it has a rigidbody
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerVelocity = playerRb.linearVelocity;
            // Simple prediction - aim where the player will be in a fraction of a second
            playerPos += playerVelocity * 0.5f;
        }

        Vector3 directionToPlayer = (playerPos - missileFirePoint.position).normalized;

        // Create missile aligned with the fire point's forward direction
        // but initialized with the direction to player
        GameObject missile = Instantiate(missilePrefab, missileFirePoint.position,
                                        missileFirePoint.rotation);

        // Initialize the BossMissile component
        BossMissile bossMissile = missile.GetComponent<BossMissile>();
        if (bossMissile != null)
        {
            bossMissile.Initialize(directionToPlayer, cylinderTransform);
            bossMissile.explosionDamage = missileDamage;
            bossMissile.speed = missileSpeed;
            // Removed the line setting target property that doesn't exist
        }
        else
        {
            // If no BossMissile component, add a simple force
            Rigidbody missileRb = missile.GetComponent<Rigidbody>();
            if (missileRb != null)
            {
                missileRb.linearVelocity = directionToPlayer * missileSpeed;
            }
        }

        // Play missile launch sound
        if (missileLaunchSound != null)
        {
            audioSource.PlayOneShot(missileLaunchSound);
        }
    }

    // Visualize firing angle in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // Draw forward detection cone
        if (maxFiringAngle > 0)
        {
            DrawArc(transform.position, transform.forward, maxFiringAngle, 3f);
        }

        // Draw missile fire direction
        if (missileFirePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(missileFirePoint.position, missileFirePoint.position + missileFirePoint.forward * 3f);
        }
    }

    // Helper method to visualize arc
    void DrawArc(Vector3 position, Vector3 direction, float angle, float radius)
    {
        Vector3 forward = direction.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float angleRadians = angle * Mathf.Deg2Rad;
        int segments = 15;

        Vector3 prevPoint = position + forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = angleRadians * t;
            Vector3 point = position + (forward * Mathf.Cos(currentAngle) + right * Mathf.Sin(currentAngle)) * radius;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        prevPoint = position + forward * radius;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = angleRadians * t;
            Vector3 point = position + (forward * Mathf.Cos(currentAngle) - right * Mathf.Sin(currentAngle)) * radius;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}