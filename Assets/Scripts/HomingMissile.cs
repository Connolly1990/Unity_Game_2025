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
    public int explosionDamage = 2;
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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (engineSound != null || explosionSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;

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

        if (cylinder != null)
        {
            cylinderTransform = cylinder;
            cylinderRadius = cylinder.localScale.x * 0.5f;
        }

        FindNearestEnemy();
    }

    void Update()
    {
        if (!isInitialized) return;

        currentLifetime += Time.deltaTime;
        if (currentLifetime >= maxLifetime)
        {
            Explode();
            return;
        }

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
            rb.linearVelocity = transform.forward * speed * initialBoostMultiplier;
        }
        else
        {
            if (targetTransform != null)
            {
                Vector3 targetDirection = (targetTransform.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                rb.linearVelocity = transform.forward * speed;
            }
            else
            {
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
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Check for all enemy types
            FlyingEnemy flyingEnemy = collision.gameObject.GetComponent<FlyingEnemy>();
            BossEnemy bossEnemy = collision.gameObject.GetComponent<BossEnemy>();
            SuicideBomber suicideBomber = collision.gameObject.GetComponent<SuicideBomber>();
            GroundEnemy groundEnemy = collision.gameObject.GetComponent<GroundEnemy>();

            if (flyingEnemy != null)
            {
                flyingEnemy.TakeDamage((int)explosionDamage);
            }
            else if (bossEnemy != null)
            {
                bossEnemy.TakeDamage((int)explosionDamage);
            }
            else if (suicideBomber != null)
            {
                suicideBomber.TakeDamage((int)explosionDamage);
            }
            else if (groundEnemy != null)
            {
                groundEnemy.TakeDamage((int)explosionDamage);
            }

            Explode();
        }
        else if (explosionLayerMask.value == 0 ||
            ((1 << collision.gameObject.layer) & explosionLayerMask.value) != 0)
        {
            Explode();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Check for all enemy types
            FlyingEnemy flyingEnemy = other.GetComponent<FlyingEnemy>();
            BossEnemy bossEnemy = other.GetComponent<BossEnemy>();
            SuicideBomber suicideBomber = other.GetComponent<SuicideBomber>();
            GroundEnemy groundEnemy = other.GetComponent<GroundEnemy>();

            if (flyingEnemy != null)
            {
                flyingEnemy.TakeDamage((int)explosionDamage);
            }
            else if (bossEnemy != null)
            {
                bossEnemy.TakeDamage((int)explosionDamage);
            }
            else if (suicideBomber != null)
            {
                suicideBomber.TakeDamage((int)explosionDamage);
            }
            else if (groundEnemy != null)
            {
                groundEnemy.TakeDamage((int)explosionDamage);
            }

            Explode();
        }
    }

    void Explode()
    {
        if (isExploding) return;
        isExploding = true;

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        if (audioSource != null && explosionSound != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.PlayOneShot(explosionSound);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayerMask);
        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Enemy"))
            {
                // Check for all enemy types in explosion radius
                FlyingEnemy flyingEnemy = hit.GetComponent<FlyingEnemy>();
                BossEnemy bossEnemy = hit.GetComponent<BossEnemy>();
                SuicideBomber suicideBomber = hit.GetComponent<SuicideBomber>();
                GroundEnemy groundEnemy = hit.GetComponent<GroundEnemy>();

                int damage = CalculateDamageByDistance(hit.transform.position);

                if (flyingEnemy != null)
                {
                    flyingEnemy.TakeDamage(damage);
                }
                else if (bossEnemy != null)
                {
                    bossEnemy.TakeDamage(damage);
                }
                else if (suicideBomber != null)
                {
                    suicideBomber.TakeDamage(damage);
                }
                else if (groundEnemy != null)
                {
                    groundEnemy.TakeDamage(damage);
                }
            }

            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        if (rb != null) rb.isKinematic = true;
        if (GetComponent<Collider>() != null)
        {
            Collider col = GetComponent<Collider>();
            col.enabled = false;
        }

        if (missileTrail != null) missileTrail.SetActive(false);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        if (audioSource != null && explosionSound != null)
        {
            Destroy(gameObject, explosionSound.length);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    int CalculateDamageByDistance(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        float damagePercent = 1f - Mathf.Clamp01(distance / explosionRadius);
        return Mathf.Max(1, (int)(explosionDamage * damagePercent));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}