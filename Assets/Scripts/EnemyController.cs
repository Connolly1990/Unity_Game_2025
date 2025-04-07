using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public enum EnemyType { Ground, Flying, SuicideBomber }

    [Header("Enemy Type")]
    public EnemyType enemyType = EnemyType.Ground;

    [Header("Basic Stats")]
    public float health = 3f;
    public float moveSpeed = 3f;
    public float attackRange = 10f;
    public int damageAmount = 1;

    [Header("Movement Settings")]
    public float stopDistance = 3f;
    public float flyingHeight = 3f; // Added for flying enemies

    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public float attackCooldown = 2f;
    public float shotAccuracy = 0.8f;
    public int shotsPerVolley = 1;
    public float angleBetweenShots = 15f;

    [Header("Suicide Bomber Settings")]
    public float blinkFrequency = 0.5f;
    public float explosionRadius = 5f;
    public float fadeDuration = 0.2f;

    private Transform player;
    private Transform cylinderTransform;
    private Transform firePoint;
    private float cooldownTimer = 0f;
    private float currentAngle;
    private bool isBlinking = false;
    private Renderer[] enemyRenderers;
    private Material[] originalMaterials;
    private float cylinderRadius;
    private Rigidbody rb;
    private float targetHeight;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody>();
        cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;

        if (cylinderTransform != null)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;
            // Calculate initial angle based on current position
            Vector3 toEnemy = transform.position - cylinderTransform.position;
            currentAngle = Mathf.Atan2(toEnemy.x, toEnemy.z);

            // Set initial height based on enemy type
            targetHeight = enemyType == EnemyType.Flying ? flyingHeight : 0.5f;
        }

        // Create fire point if not found
        firePoint = transform.Find("FirePoint");
        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(transform);
            firePoint.localPosition = new Vector3(0, 0.5f, 0.5f);
        }

        enemyRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            originalMaterials[i] = enemyRenderers[i].material;
        }

        InitializeEnemyType();
    }

    private void InitializeEnemyType()
    {
        switch (enemyType)
        {
            case EnemyType.Ground:
                shotsPerVolley = 3;
                if (rb != null)
                {
                    rb.useGravity = false; // Changed from true since we're on a cylinder
                    rb.constraints = RigidbodyConstraints.None;
                }
                break;
            case EnemyType.Flying:
                shotsPerVolley = 1;
                if (rb != null)
                {
                    rb.useGravity = false;
                    targetHeight = flyingHeight;
                }
                stopDistance = 5f;
                break;
            case EnemyType.SuicideBomber:
                if (rb != null)
                {
                    rb.useGravity = false;
                    targetHeight = flyingHeight / 2f; // Lower height for bombers
                }
                StartCoroutine(BlinkRoutine());
                break;
        }
    }

    private void Update()
    {
        if (player == null) return;

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (enemyType == EnemyType.SuicideBomber && distanceToPlayer < 2f)
        {
            Explode();
            return;
        }

        if (distanceToPlayer <= attackRange && cooldownTimer <= 0)
        {
            Attack();
        }
    }

    private void FixedUpdate()
    {
        if (player == null || cylinderTransform == null) return;

        // Calculate angle to player (using cylinder coordinates)
        Vector3 playerPos = player.position - cylinderTransform.position;
        float playerAngle = Mathf.Atan2(playerPos.x, playerPos.z);

        // Calculate shortest angular distance (handles wrapping around the cylinder)
        float angleDiff = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, playerAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        // Check if we're at stopping distance (use angular distance on cylinder)
        float angularDistance = Mathf.Abs(angleDiff) * cylinderRadius;
        if (angularDistance > stopDistance / 2f)
        {
            // Move toward player
            currentAngle += Mathf.Sign(angleDiff) * moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        }

        // Normalize angle to prevent overflow
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Calculate position on cylinder surface
        Vector3 newPosition = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            targetHeight,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        // Face toward player (tangent to cylinder)
        Vector3 toCenter = cylinderTransform.position - newPosition;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        // Determine which direction on the tangent to look based on player position
        Vector3 toPlayer = player.position - newPosition;
        float dotProduct = Vector3.Dot(tangent, toPlayer);
        Vector3 facingDirection = dotProduct > 0 ? tangent : -tangent;

        Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);

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

    private void Attack()
    {
        cooldownTimer = attackCooldown;

        // Calculate tangent direction for shooting
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        // Determine forward direction based on player position
        Vector3 toPlayer = player.position - transform.position;
        float dotProduct = Vector3.Dot(tangent, toPlayer);
        Vector3 shootDirection = dotProduct > 0 ? tangent : -tangent;

        for (int i = 0; i < shotsPerVolley; i++)
        {
            Vector3 shotDirection = shootDirection;

            if (shotsPerVolley > 1)
            {
                float angleOffset = (i - (shotsPerVolley - 1) / 2f) * angleBetweenShots;
                shotDirection = Quaternion.Euler(0, angleOffset, 0) * shotDirection;
            }

            if (shotAccuracy < 1.0f)
            {
                float inaccuracy = (1f - shotAccuracy) * 30f;
                shotDirection = Quaternion.Euler(
                    Random.Range(-inaccuracy, inaccuracy),
                    Random.Range(-inaccuracy, inaccuracy),
                    0
                ) * shotDirection;
            }

            if (projectilePrefab != null)
            {
                GameObject projectile = Instantiate(
                    projectilePrefab,
                    firePoint.position,
                    Quaternion.LookRotation(shotDirection)
                );

                EnemyLaser laser = projectile.GetComponent<EnemyLaser>();
                if (laser != null)
                {
                    laser.Initialize(
                        cylinderTransform,
                        shotDirection,
                        15f,
                        damageAmount,
                        false
                    );
                }
            }
        }
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            isBlinking = !isBlinking;

            if (isBlinking)
            {
                // Fade out
                yield return FadeEnemy(0.3f, fadeDuration);
            }
            else
            {
                // Fade in
                yield return FadeEnemy(1f, fadeDuration);
            }

            yield return new WaitForSeconds(blinkFrequency);
        }
    }

    private IEnumerator FadeEnemy(float targetAlpha, float duration)
    {
        float elapsed = 0f;
        Color[] startColors = new Color[enemyRenderers.Length];

        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            startColors[i] = enemyRenderers[i].material.color;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < enemyRenderers.Length; i++)
            {
                Color newColor = startColors[i];
                newColor.a = Mathf.Lerp(startColors[i].a, targetAlpha, t);
                enemyRenderers[i].material.color = newColor;
            }

            yield return null;
        }
    }

    private void Explode()
    {
        // Create a better explosion effect with proper size and duration
        GameObject explosion = new GameObject("ExplosionEffect");
        explosion.transform.position = transform.position;

        // Add particle system component
        ParticleSystem explosionPS = explosion.AddComponent<ParticleSystem>();

        // Configure main module
        var main = explosionPS.main;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.red);
        main.startSize = explosionRadius * 0.4f; // Smaller size to avoid filling screen
        main.startLifetime = 0.5f; // Shorter lifetime
        main.startSpeed = explosionRadius * 0.8f;
        main.duration = 0.3f;
        main.loop = false;

        // Configure emission
        var emission = explosionPS.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]{
        new ParticleSystem.Burst(0f, 30f)
    });

        // Configure shape
        var shape = explosionPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Add explosion light (optional)
        GameObject lightObj = new GameObject("ExplosionLight");
        lightObj.transform.parent = explosion.transform;
        Light explosionLight = lightObj.AddComponent<Light>();
        explosionLight.color = new Color(1f, 0.7f, 0f);
        explosionLight.intensity = 3f;
        explosionLight.range = explosionRadius * 1.5f;

        // Animate light fade out
        StartCoroutine(FadeExplosionLight(explosionLight, 0.5f));

        // Damage the player if within explosion radius
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageAmount * 2);
                }
            }
        }

        // Destroy explosion and enemy
        Destroy(explosion, 2f);
        Destroy(gameObject);  // Suicide bomber disappears after explosion
    }

    private IEnumerator FadeExplosionLight(Light light, float duration)
    {
        float elapsed = 0f;
        float initialIntensity = light.intensity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(initialIntensity, 0f, elapsed / duration);
            yield return null;
        }

        light.intensity = 0f;
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
        // Suicide bombers don't need death effects since they explode
        if (enemyType != EnemyType.SuicideBomber)
        {
            CreateDeathEffect();
        }

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