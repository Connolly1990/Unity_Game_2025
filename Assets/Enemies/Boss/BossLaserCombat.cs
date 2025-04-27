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
    public float detectionRange = 15f;
    public AudioClip laserShotSound;

    // Private variables
    private Transform player;
    private Transform cylinderTransform;
    private float cylinderRadius;
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private bool isInitialized = false;

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
        if (!isInitialized || player == null) return;

        // Check if player is in range
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange && Time.time >= nextFireTime)
        {
            StartCoroutine(FireLaserBurst());
            nextFireTime = Time.time + burstFireRate;
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

        // Calculate direction to player
        Vector3 directionToPlayer = (player.position - firePoint.position).normalized;

        // Create laser
        GameObject laser = Instantiate(laserPrefab, firePoint.position, Quaternion.LookRotation(directionToPlayer));

        // Initialize the BossLaser component
        BossLaser bossLaser = laser.GetComponent<BossLaser>();
        if (bossLaser != null)
        {
            bossLaser.Initialize(cylinderTransform, directionToPlayer, laserSpeed, laserDamage, false);
        }
        else
        {
            // If no BossLaser component, add a simple force
            Rigidbody laserRb = laser.GetComponent<Rigidbody>();
            if (laserRb != null)
            {
                laserRb.linearVelocity = directionToPlayer * laserSpeed;
            }
        }
    }

    // Visualize detection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

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
}