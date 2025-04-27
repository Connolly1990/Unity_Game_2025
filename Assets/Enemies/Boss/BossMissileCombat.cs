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
    public float detectionRange = 20f;
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
        nextFireTime = Time.time + Random.Range(2f, missileFireRate);
    }

    void Update()
    {
        if (!isInitialized || player == null || isFiringSequence) return;

        // Check if player is in range
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange && Time.time >= nextFireTime)
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

        // Direction to player
        Vector3 directionToPlayer = (player.position - missileFirePoint.position).normalized;

        // Create missile
        GameObject missile = Instantiate(missilePrefab, missileFirePoint.position,
                                        Quaternion.LookRotation(directionToPlayer));

        // Initialize the BossMissile component
        BossMissile bossMissile = missile.GetComponent<BossMissile>();
        if (bossMissile != null)
        {
            bossMissile.Initialize(directionToPlayer, cylinderTransform);
            bossMissile.explosionDamage = missileDamage;
            bossMissile.speed = missileSpeed;
        }

        // Play missile launch sound
        if (missileLaunchSound != null)
        {
            audioSource.PlayOneShot(missileLaunchSound);
        }
    }

    // Visualize detection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw missile fire direction
        if (missileFirePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(missileFirePoint.position, missileFirePoint.position + missileFirePoint.forward * 3f);
        }
    }
}