using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class BossMissile : MonoBehaviour
{
    [Header("Missile Properties")]
    public float speed = 12f;
    public float rotationSpeed = 180f;
    public float maxLifetime = 8f;
    public float initialBoostDuration = 0.5f;
    public float initialBoostMultiplier = 1.5f;
    public float detectionRadius = 30f;
    public float cylinderTrackingFactor = 0.8f; // How well the missile stays on cylinder (0-1)

    [Header("Explosion")]
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    public int explosionDamage = 2;
    public GameObject explosionEffect;
    public LayerMask explosionLayerMask;
    public AudioClip explosionSound;

    [Header("Effects")]
    public GameObject missileTrail;
    public AudioClip engineSound;

    private Rigidbody rb;
    private Transform targetTransform;
    private float currentLifetime = 0f;
    private bool isInitialized = false;
    private AudioSource audioSource;
    private bool isExploding = false;
    private Vector3 initialDirection;
    private Transform cylinderTransform;
    private float cylinderRadius;
    private float currentAngle;
    private float initialHeight;
    private bool isBoostActive = false;
    private float boostEndTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Set up audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio
        audioSource.spatialBlend = 0.8f; // Mostly 3D but some 2D
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 50f;

        // Setup rigidbody
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Initialize(Vector3 direction, Transform cylinder)
    {
        initialDirection = direction;
        cylinderTransform = cylinder;

        if (cylinderTransform != null)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;

            // Calculate current angle on cylinder
            Vector3 positionOnXZ = transform.position;
            positionOnXZ.y = cylinderTransform.position.y;
            Vector3 dirFromCenter = positionOnXZ - cylinderTransform.position;
            currentAngle = Mathf.Atan2(dirFromCenter.x, dirFromCenter.z);
        }

        initialHeight = transform.position.y;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetTransform = playerObj.transform;
        }

        // Start boost effect
        isBoostActive = true;
        boostEndTime = Time.time + initialBoostDuration;

        // Start engine sound
        if (engineSound != null)
        {
            audioSource.clip = engineSound;
            audioSource.Play();
        }

        // Activate trail effect
        if (missileTrail != null)
        {
            missileTrail.SetActive(true);
        }

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || isExploding) return;

        // Update lifetime and check for self-destruction
        currentLifetime += Time.deltaTime;
        if (currentLifetime >= maxLifetime)
        {
            Explode();
            return;
        }

        // Check if boost has ended
        if (isBoostActive && Time.time > boostEndTime)
        {
            isBoostActive = false;
        }
    }

    void FixedUpdate()
    {
        if (!isInitialized || isExploding) return;

        // Get direction to target
        Vector3 targetDirection;

        if (targetTransform != null)
        {
            targetDirection = (targetTransform.position - transform.position).normalized;
        }
        else
        {
            // If no target, continue in initial direction
            targetDirection = initialDirection;
        }

        // Calculate current position on cylinder
        Vector3 projectedPosition = transform.position;
        projectedPosition.y = cylinderTransform.position.y;

        Vector3 centerToMissile = projectedPosition - cylinderTransform.position;
        float distanceFromCenter = centerToMissile.magnitude;

        // Adjust direction to stay on cylinder surface
        if (cylinderTransform != null && cylinderTrackingFactor > 0f)
        {
            // Calculate ideal distance from center
            float idealDistance = cylinderRadius;

            // Calculate correction vector to pull missile toward cylinder surface
            Vector3 correctionDirection = Vector3.zero;

            if (distanceFromCenter > 0.1f) // Avoid division by zero
            {
                Vector3 normalizedCenterToMissile = centerToMissile / distanceFromCenter;

                if (distanceFromCenter > idealDistance)
                {
                    // Pull inward
                    correctionDirection = -normalizedCenterToMissile;
                }
                else if (distanceFromCenter < idealDistance)
                {
                    // Push outward
                    correctionDirection = normalizedCenterToMissile;
                }

                // Blend original direction with correction
                targetDirection = Vector3.Lerp(
                    targetDirection,
                    correctionDirection,
                    cylinderTrackingFactor * Time.fixedDeltaTime * 5f
                );
            }

            // Also try to maintain initial height
            float heightDifference = initialHeight - transform.position.y;
            Vector3 heightCorrection = new Vector3(0, heightDifference, 0).normalized;
            targetDirection = Vector3.Lerp(
                targetDirection,
                heightCorrection,
                cylinderTrackingFactor * 0.3f * Time.fixedDeltaTime
            );
        }

        // Apply rotation
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        rb.rotation = Quaternion.RotateTowards(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        // Apply forward movement
        float currentSpeed = speed;
        if (isBoostActive)
        {
            currentSpeed *= initialBoostMultiplier;
        }

        rb.linearVelocity = transform.forward * currentSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't explode if hit by another missile
        if (collision.gameObject.GetComponent<BossMissile>() != null)
        {
            return;
        }

        Explode();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Explode if hit by player laser
        if (other.CompareTag("PlayerLaser"))
        {
            Destroy(other.gameObject);
            Explode();
        }
    }

    private void Explode()
    {
        if (isExploding) return;
        isExploding = true;

        // Stop movement
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Stop audio
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound);
        }

        // Create explosion effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // Apply explosion force to nearby objects
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayerMask);
        foreach (Collider hit in colliders)
        {
            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0.5f);
            }

            // Damage player if hit
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(explosionDamage);
                }
            }
        }

        // Disable missile renderer
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Disable trail effect
        if (missileTrail != null)
        {
            missileTrail.SetActive(false);
        }

        // Disable colliders
        Collider[] missileColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in missileColliders)
        {
            col.enabled = false;
        }

        // Destroy after sound finishes
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        float delay = explosionSound != null ? explosionSound.length : 1.0f;
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Show explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Show direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}