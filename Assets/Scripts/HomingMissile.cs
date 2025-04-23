using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class HomingMissile : MonoBehaviour
{
    [Header("Missile Properties")]
    public float speed = 15f;
    public float rotationSpeed = 200f;
    public float maxLifetime = 10f;
    public float initialBoostDuration = 0.5f;
    public float initialBoostMultiplier = 1.5f;
    public float detectionRadius = 30f;
    public float targetUpdateInterval = 0.5f;

    [Header("Explosion")]
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    public int explosionDamage = 2; // Changed to int to match FlyingEnemy.TakeDamage
    public GameObject explosionEffect;
    public LayerMask explosionLayerMask;
    public AudioClip explosionSound;

    [Header("Effects")]
    public GameObject missileTrail;
    public AudioClip engineSound;

    private Rigidbody rb;
    private Transform targetTransform;
    private float nextTargetSearchTime;
    private float currentLifetime = 0f;
    private bool isInitialized = false;
    private AudioSource audioSource;
    private bool isExploding = false;

    private Transform cylinderTransform;
    private float cylinderRadius;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Set up audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (engineSound != null || explosionSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound

            if (engineSound != null)
            {
                audioSource.clip = engineSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
    }

    public void Initialize(Vector3 initialDirection, Transform cylinder = null)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.linearVelocity = initialDirection.normalized * speed * initialBoostMultiplier;
        transform.forward = initialDirection.normalized;

        rb.useGravity = false;
        rb.linearDamping = 0.5f;

        isInitialized = true;
        nextTargetSearchTime = 0f;

        // Store cylinder reference for Resogun style game
        if (cylinder != null)
        {
            cylinderTransform = cylinder;
            cylinderRadius = cylinder.localScale.x * 0.5f;
        }

        // Initial search for target
        FindNearestEnemy();
    }

    void Update()
    {
        if (!isInitialized) return;

        // Update lifetime and destroy if exceeded
        currentLifetime += Time.deltaTime;
        if (currentLifetime >= maxLifetime)
        {
            Explode();
            return;
        }

        // Update target search periodically
        if (Time.time >= nextTargetSearchTime)
        {
            FindNearestEnemy();
            nextTargetSearchTime = Time.time + targetUpdateInterval;
        }
    }

    void FixedUpdate()
    {
        if (!isInitialized || isExploding) return;

        if (currentLifetime <= initialBoostDuration)
        {
            // Initial boost phase - just maintain speed and direction
            rb.linearVelocity = transform.forward * speed * initialBoostMultiplier;
        }
        else
        {
            // Normal homing behavior
            if (targetTransform != null)
            {
                // Calculate direction to target
                Vector3 targetDirection = (targetTransform.position - transform.position).normalized;

                // Rotate towards target
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Apply forward thrust
                rb.linearVelocity = transform.forward * speed;
            }
            else
            {
                // No target found, maintain current direction
                rb.linearVelocity = transform.forward * speed;
            }
        }
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance && distance <= detectionRadius)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        targetTransform = closestEnemy;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we hit an enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Direct hit damage before explosion
            // Check for FlyingEnemy script instead of EnemyHealth
            FlyingEnemy enemy = collision.gameObject.GetComponent<FlyingEnemy>();
            if (enemy != null)
            {
                // Apply direct hit damage
                enemy.TakeDamage((int)explosionDamage);
            }

            Explode();
        }
        // Hit something else valid for explosion
        else if (explosionLayerMask.value == 0 ||
            ((1 << collision.gameObject.layer) & explosionLayerMask.value) != 0)
        {
            Explode();
        }
    }

    // Add trigger-based collision as well for more reliable hit detection
    void OnTriggerEnter(Collider other)
    {
        // Check if we hit an enemy
        if (other.CompareTag("Enemy"))
        {
            // Direct hit damage before explosion
            // Check for FlyingEnemy script instead of EnemyHealth
            FlyingEnemy enemy = other.GetComponent<FlyingEnemy>();
            if (enemy != null)
            {
                // Apply direct hit damage
                enemy.TakeDamage((int)explosionDamage);
            }

            Explode();
        }
    }

    void Explode()
    {
        if (isExploding) return;
        isExploding = true;

        // Create explosion effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // Play explosion sound
        if (audioSource != null && explosionSound != null)
        {
            audioSource.Stop(); // Stop engine sound
            audioSource.loop = false;
            audioSource.PlayOneShot(explosionSound);
        }

        // Apply explosion force and damage to nearby objects
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayerMask);
        foreach (Collider hit in colliders)
        {
            // Apply damage to enemies
            if (hit.CompareTag("Enemy"))
            {
                // If there's a FlyingEnemy component, deal damage
                FlyingEnemy enemy = hit.GetComponent<FlyingEnemy>();
                if (enemy != null)
                {
                    int damage = CalculateDamageByDistance(hit.transform.position);
                    enemy.TakeDamage(damage);
                }
            }

            // Apply force to rigidbodies
            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        // Destroy missile components but keep the GameObject until sound finishes
        if (rb != null) rb.isKinematic = true;
        if (GetComponent<Collider>() != null)
        {
            Collider col = GetComponent<Collider>();
            col.enabled = false;
        }

        if (missileTrail != null) missileTrail.SetActive(false);

        // Hide the missile visuals
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        // Schedule final destruction
        if (audioSource != null && explosionSound != null)
        {
            // Wait for explosion sound to finish
            Destroy(gameObject, explosionSound.length);
        }
        else
        {
            // No sound, destroy immediately
            Destroy(gameObject);
        }
    }

    int CalculateDamageByDistance(Vector3 targetPosition)
    {
        // Calculate damage falloff based on distance from explosion center
        float distance = Vector3.Distance(transform.position, targetPosition);
        float damagePercent = 1f - Mathf.Clamp01(distance / explosionRadius);
        return Mathf.Max(1, (int)(explosionDamage * damagePercent));
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}