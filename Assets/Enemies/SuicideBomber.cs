using UnityEngine;
using System.Collections;

public class SuicideBomber : MonoBehaviour
{
    [Header("Basic Stats")]
    public float health = 3f;
    public float moveSpeed = 5f; // Usually faster than other enemies
    public int damageAmount = 2; // Higher damage since it's a suicide attack

    [Header("Movement Settings")]
    public float flyingHeight = 2f; // Lower than flying enemies
    public float explosionDistance = 2f; // Distance at which it explodes

    [Header("Explosion Settings")]
    public float blinkFrequency = 0.5f;
    public float explosionRadius = 5f;
    public float fadeDuration = 0.2f;

    private Transform playerTransform;
    private Transform cylinderTransform;
    private float cylinderRadius;
    private float currentAngle;
    private bool isBlinking = false;
    private Renderer[] enemyRenderers;
    private Material[] originalMaterials;
    private Rigidbody rb;
    private int facingDirection = 1;  // 1 or -1 for direction on tangent

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;
        rb = GetComponent<Rigidbody>();

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

        // Disable gravity and start blinking
        if (rb != null)
        {
            rb.useGravity = false;
        }

        StartCoroutine(BlinkRoutine());
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if close enough to explode
        if (distanceToPlayer <= explosionDistance)
        {
            Explode();
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

        // Always move toward player (suicide bombers don't stop)
        currentAngle += Mathf.Sign(angleDiff) * moveSpeed * Time.fixedDeltaTime / cylinderRadius;

        // Normalize angle to prevent overflow
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Calculate position on cylinder surface
        Vector3 newPosition = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            flyingHeight, // Suicide bombers hover at medium height
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
        // Create explosion effect
        GameObject explosion = new GameObject("ExplosionEffect");
        explosion.transform.position = transform.position;

        // Add particle system component
        ParticleSystem explosionPS = explosion.AddComponent<ParticleSystem>();

        // Configure main module
        var main = explosionPS.main;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.red);
        main.startSize = explosionRadius * 0.4f;
        main.startLifetime = 0.5f;
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

        // Add explosion light
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
        Destroy(gameObject);
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
            Explode(); // Suicide bomber explodes when killed
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
}