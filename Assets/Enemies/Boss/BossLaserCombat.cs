using UnityEngine;
using System.Collections;

public class BossLaserCombat : MonoBehaviour
{
    [Header("References")]
    public Transform[] laserFirePoints; // Array of 3 fire points for burst effect
    public GameObject laserPrefab;

    [Header("Laser Attack Settings")]
    public float burstFireRate = 2f; // Time between bursts
    public float burstDelay = 0.05f; // Delay between each laser in burst
    public int laserDamage = 1;
    public float laserSpeed = 12f;
    public float maxFiringAngle = 45f; // Maximum angle to still fire at player
    public AudioClip laserShotSound;

    // Private variables
    private Transform player;
    private Transform cylinderTransform;
    private float cylinderRadius;
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private bool isInitialized = false;
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
        nextFireTime = Time.time + Random.Range(1f, burstFireRate);
    }

    void Update()
    {
        if (!isInitialized || player == null || bossController == null || bossController.isInvulnerable) return;

        // Check if it's time to fire
        if (Time.time >= nextFireTime)
        {
            // Check if boss is facing player within a reasonable angle
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            if (angle <= maxFiringAngle)
            {
                StartCoroutine(FireLaserBurst());
                nextFireTime = Time.time + burstFireRate;
            }
        }
    }

    IEnumerator FireLaserBurst()
    {
        // Make sure we have valid fire points
        if (laserFirePoints == null || laserFirePoints.Length == 0)
        {
            Debug.LogWarning("No laser fire points assigned to BossLaserCombat!");
            yield break;
        }

        // Fire from each fire point with a slight delay
        foreach (Transform firePoint in laserFirePoints)
        {
            if (firePoint != null)
            {
                FireLaser(firePoint);

                // Play sound
                if (laserShotSound != null)
                {
                    audioSource.PlayOneShot(laserShotSound);
                }

                yield return new WaitForSeconds(burstDelay);
            }
        }
    }

    void FireLaser(Transform firePoint)
    {
        if (laserPrefab == null || player == null) return;

        // Calculate direction to player with slight spread
        Vector3 baseDirection = (player.position - firePoint.position).normalized;

        // Add slight variation to make it more game-like
        Vector3 spreadDirection = baseDirection + new Vector3(
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.05f, 0.05f)
        ).normalized;

        // Create laser
        GameObject laser = Instantiate(laserPrefab, firePoint.position, Quaternion.LookRotation(spreadDirection));

        // Initialize the BossLaser component
        BossLaser bossLaser = laser.GetComponent<BossLaser>();
        if (bossLaser != null)
        {
            bossLaser.Initialize(cylinderTransform, spreadDirection, laserSpeed, laserDamage, false);
        }
        else
        {
            // If no BossLaser component, add a simple force
            Rigidbody laserRb = laser.GetComponent<Rigidbody>();
            if (laserRb != null)
            {
                laserRb.linearVelocity = spreadDirection * laserSpeed;
            }
        }
    }

    // Visualize firing angle in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        // Draw forward detection cone
        if (maxFiringAngle > 0)
        {
            DrawArc(transform.position, transform.forward, maxFiringAngle, 2f);
        }

        // Draw laser fire directions
        if (laserFirePoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform firePoint in laserFirePoints)
            {
                if (firePoint != null)
                {
                    Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.forward * 2f);
                }
            }
        }
    }

    // Helper method to visualize arc
    void DrawArc(Vector3 position, Vector3 direction, float angle, float radius)
    {
        Vector3 forward = direction.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right).normalized;

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