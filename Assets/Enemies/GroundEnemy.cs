using UnityEngine;
using System.Collections;

public class GroundEnemy : MonoBehaviour
{
    [Header("Basic Stats")]
    public float health = 3f;
    public float moveSpeed = 3f;
    public float attackRange = 10f;
    public int damageAmount = 1;

    [Header("Movement Settings")]
    public float stopDistance = 3f;

    [Header("Shooting Settings")]
    public GameObject laserPrefab;
    public float attackCooldown = 2f;
    public float shotAccuracy = 0.8f;
    public int shotsPerVolley = 3;
    public float angleBetweenShots = 15f;

    private Transform playerTransform;
    private Transform cylinderTransform;
    private Transform enemyFirePoint;
    private float cylinderRadius;
    private float currentAngle;
    private float cooldownTimer = 0f;
    private Rigidbody rb;
    private Renderer[] enemyRenderers;
    private Material[] originalMaterials;
    private int facingDirection = 1;  // 1 or -1 for direction on tangent

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;
        rb = GetComponent<Rigidbody>();

        // Create fire point if not found
        enemyFirePoint = transform.Find("FirePoint");
        if (enemyFirePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            enemyFirePoint = firePointObj.transform;
            enemyFirePoint.SetParent(transform);
            enemyFirePoint.localPosition = new Vector3(0, 0.5f, 0.5f);
        }

        if (cylinderTransform != null)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;

            // Calculate initial angle based on current position
            Vector3 toEnemy = transform.position - cylinderTransform.position;
            currentAngle = Mathf.Atan2(toEnemy.x, toEnemy.z);
        }

        enemyRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            originalMaterials[i] = enemyRenderers[i].material;
        }

        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Handle attack cooldown
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if in attack range and cooled down
        if (distanceToPlayer <= attackRange && cooldownTimer <= 0)
        {
            FireAtPlayer();
        }
    }

    private void FixedUpdate()
    {
        if (playerTransform == null || cylinderTransform == null) return;

        // Calculate angle to player (using cylinder coordinates)
        Vector3 playerPos = playerTransform.position - cylinderTransform.position;
        float playerAngle = Mathf.Atan2(playerPos.x, playerPos.z);

        // Calculate shortest angular distance (handles wrapping around the cylinder)
        float angleDiff = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, playerAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        // Check if we're at stopping distance (use angular distance on cylinder)
        float angularDistance = Mathf.Abs(angleDiff) * cylinderRadius;
        if (angularDistance > stopDistance)
        {
            // Move toward player
            currentAngle += Mathf.Sign(angleDiff) * moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        }

        // Normalize angle to prevent overflow
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Calculate position on cylinder surface
        Vector3 newPosition = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            0.5f, // Ground enemies stay at a small height
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        // Face toward player (tangent to cylinder)
        Vector3 toCenter = cylinderTransform.position - newPosition;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        // Determine which direction on the tangent to look based on player position
        Vector3 toPlayer = playerTransform.position - newPosition;
        float dotProduct = Vector3.Dot(tangent, toPlayer);
        facingDirection = dotProduct > 0 ? 1 : -1;
        Vector3 facingDirectionVector = tangent * facingDirection;

        Quaternion targetRotation = Quaternion.LookRotation(facingDirectionVector, Vector3.up);

        // Update position and rotation
        if (rb != null)
        {
            rb.MovePosition(newPosition);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime));
        }
        else
        {
            transform.position = newPosition;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    private void FireAtPlayer()
    {
        if (enemyFirePoint == null || laserPrefab == null || playerTransform == null || cylinderTransform == null) return;

        // Set cooldown
        cooldownTimer = attackCooldown;

        // Get the tangent direction at our position
        Vector3 toCenter = transform.position - cylinderTransform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        // Fire in the direction we're facing
        Vector3 laserDir = tangent * facingDirection;

        // Make sure the fire point is positioned on the cylinder
        Vector3 firePointPosition = ProjectPointToCylinder(enemyFirePoint.position);

        for (int i = 0; i < shotsPerVolley; i++)
        {
            Vector3 shotDirection = laserDir;

            if (shotsPerVolley > 1)
            {
                float angleOffset = (i - (shotsPerVolley - 1) / 2f) * angleBetweenShots;
                shotDirection = Quaternion.Euler(0, angleOffset, 0) * shotDirection;
            }

            // Add inaccuracy if shotAccuracy is less than 1
            if (shotAccuracy < 1.0f)
            {
                float inaccuracy = (1f - shotAccuracy) * 30f;
                shotDirection = Quaternion.Euler(
                    Random.Range(-inaccuracy, inaccuracy),
                    Random.Range(-inaccuracy, inaccuracy),
                    0
                ) * shotDirection;
            }

            // Create the laser
            GameObject laser = Instantiate(laserPrefab, firePointPosition, Quaternion.LookRotation(shotDirection, Vector3.up));

            // Initialize laser with cylinder reference and direction
            EnemyLaser enemyLaser = laser.GetComponent<EnemyLaser>();
            if (enemyLaser != null)
            {
                enemyLaser.Initialize(cylinderTransform, shotDirection, 15f, damageAmount, false);
            }
        }
    }

    // Helper method to project a point onto the cylinder surface
    private Vector3 ProjectPointToCylinder(Vector3 point)
    {
        if (cylinderTransform == null) return point;

        Vector3 dirToCylinder = point - cylinderTransform.position;
        dirToCylinder.y = 0; // Project onto XZ plane
        dirToCylinder = dirToCylinder.normalized * cylinderRadius;

        return new Vector3(
            cylinderTransform.position.x + dirToCylinder.x,
            point.y, // Keep original height
            cylinderTransform.position.z + dirToCylinder.z
        );
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        StartCoroutine(DamageFlash());

        if (health <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlash()
    {
        foreach (Renderer renderer in enemyRenderers)
        {
            renderer.material.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            if (enemyRenderers[i] != null && originalMaterials[i] != null)
            {
                enemyRenderers[i].material.color = originalMaterials[i].color;
            }
        }
    }

    private void Die()
    {
        CreateDeathEffect();
        Destroy(gameObject);
    }

    private void CreateDeathEffect()
    {
        GameObject deathEffectObj = new GameObject("DeathEffect");
        deathEffectObj.transform.position = transform.position;
        ParticleSystem deathEffect = deathEffectObj.AddComponent<ParticleSystem>();

        var main = deathEffect.main;
        main.startColor = Color.red;
        main.startSize = 0.5f;
        main.startLifetime = 1f;
        main.startSpeed = 2f;

        var emission = deathEffect.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        deathEffect.Play();
        Destroy(deathEffectObj, 1f);
    }
}