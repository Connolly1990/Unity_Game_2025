using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SuicideBomber : MonoBehaviour
{
    [Header("References")]
    public Transform enemyModel;

    [Header("Basic Stats")]
    public int maxHealth = 2;
    public float moveSpeed = 5f; // Usually faster than other enemies
    public int damageAmount = 2; // Higher damage since it's a suicide attack

    [Header("Movement Settings")]
    public float explosionDistance = 2f; // Distance at which it explodes
    public float detectionRange = 15f; // Usually larger detection range
    public float rotationSpeed = 120f; // Faster rotation for quick targeting
    public float verticalSpeed = 2f; // Speed at which it moves vertically

    [Header("Explosion Settings")]
    public float blinkFrequency = 0.5f;
    public float explosionRadius = 5f;
    public float fadeDuration = 0.2f;

    [Header("Cylinder Boundaries")]
    public float maxHeight = 7f;
    public float minHeight = 1.5f;

    // Private variables
    private Transform playerTransform;
    private Transform cylinderTransform;
    private float cylinderRadius;
    private float currentAngle;
    private bool isBlinking = false;
    private Renderer[] enemyRenderers;
    private Color[] originalColors;
    private Rigidbody rb;
    private int currentHealth;
    private float facingDirection = 1f;  // 1 or -1 for direction on tangent
    private Vector3 currentPosition;
    private bool isExploding = false;

    private void Awake()
    {
        // Store initial position
        currentPosition = transform.position;

        // Setup rigidbody
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Find references
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;

        // Initialize health
        currentHealth = maxHealth;

        if (cylinderTransform != null)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;

            // Calculate initial angle based on current position
            Vector3 toEnemy = transform.position - cylinderTransform.position;
            currentAngle = Mathf.Atan2(toEnemy.x, toEnemy.z);
        }

        // Setup renderers
        enemyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            originalColors[i] = enemyRenderers[i].material.color;
        }

        // Start blinking effect
        StartCoroutine(BlinkRoutine());
    }

    private void Start()
    {
        // Double check references if somehow missed in Awake
        if (cylinderTransform == null)
        {
            GameObject levelObject = GameObject.FindGameObjectWithTag("Level");
            if (levelObject != null)
            {
                cylinderTransform = levelObject.transform;
            }
            else
            {
                Debug.LogError("No object with 'Level' tag found for suicide bomber!");
            }
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("No object with 'Player' tag found for suicide bomber!");
            }
        }

        // Initialize cylinder properties
        if (cylinderTransform != null)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;

            // If we're too far from the cylinder, project onto it
            float distanceToCenter = Vector3.Distance(
                new Vector3(transform.position.x, cylinderTransform.position.y, transform.position.z),
                new Vector3(cylinderTransform.position.x, cylinderTransform.position.y, cylinderTransform.position.z)
            );

            if (Mathf.Abs(distanceToCenter - cylinderRadius) > 0.5f)
            {
                // We're not on the cylinder surface, fix position
                Vector3 towardCenter = cylinderTransform.position - transform.position;
                towardCenter.y = 0;
                towardCenter.Normalize();

                Vector3 newPos = cylinderTransform.position - towardCenter * cylinderRadius;
                newPos.y = transform.position.y;
                currentPosition = newPos;
                transform.position = newPos;
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerTransform == null || cylinderTransform == null || isExploding) return;

        // Calculate player angle on cylinder
        float playerAngle = Mathf.Atan2(
            playerTransform.position.x - cylinderTransform.position.x,
            playerTransform.position.z - cylinderTransform.position.z
        );

        // Calculate angular distance to player (shortest path)
        float angleDiff = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, playerAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        // Calculate actual distance to player along cylinder
        float distanceToPlayer = Mathf.Abs(angleDiff * cylinderRadius);

        // Calculate vertical distance
        float verticalDist = Mathf.Abs(transform.position.y - playerTransform.position.y);

        // Combine distances for full detection
        float combinedDistance = Mathf.Sqrt(distanceToPlayer * distanceToPlayer + verticalDist * verticalDist);

        // Create vectors for current position and player position on cylinder
        Vector3 currentPosOnCylinder = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            transform.position.y,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        Vector3 playerPosOnCylinder = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(playerAngle),
            playerTransform.position.y,
            cylinderRadius * Mathf.Cos(playerAngle)
        );

        // Get tangent at our position
        Vector3 toCenter = cylinderTransform.position - currentPosOnCylinder;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        // Calculate relative position to determine which way to face
        Vector3 toPlayer = playerPosOnCylinder - currentPosOnCylinder;
        float dot = Vector3.Dot(tangent, toPlayer);

        // Set facing direction based on where player is
        facingDirection = (dot >= 0) ? 1f : -1f;

        // Check if close enough to explode
        if (combinedDistance <= explosionDistance)
        {
            Explode();
            return;
        }

        // Move towards player horizontally
        float moveDirection = Mathf.Sign(angleDiff);
        currentAngle += moveDirection * moveSpeed * Time.fixedDeltaTime / cylinderRadius;

        // Determine vertical movement direction
        float verticalDirection = 0;
        if (Mathf.Abs(transform.position.y - playerTransform.position.y) > 0.1f)
        {
            verticalDirection = playerTransform.position.y > transform.position.y ? 1 : -1;
        }

        // Apply vertical movement
        float newY = transform.position.y + (verticalDirection * verticalSpeed * Time.fixedDeltaTime);
        newY = Mathf.Clamp(newY, minHeight, maxHeight);

        // Update position without teleportation
        Vector3 newPositionOnCylinder = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            newY,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        // Calculate facing direction
        Vector3 facingDir = tangent * facingDirection;

        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(facingDir, Vector3.up);

        // Smoothly update position and rotation
        currentPosition = Vector3.Lerp(transform.position, newPositionOnCylinder, 0.5f);
        transform.position = currentPosition;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Normalize angle to prevent overflow
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            isBlinking = !isBlinking;

            if (isBlinking)
            {
                // Blink color to red
                Color blinkColor = Color.red;
                yield return MaterialColorPulse(blinkColor, fadeDuration);
            }
            else
            {
                // Return to normal
                yield return RestoreOriginalColors(fadeDuration);
            }

            yield return new WaitForSeconds(blinkFrequency);
        }
    }

    private IEnumerator MaterialColorPulse(Color targetColor, float duration)
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
                enemyRenderers[i].material.color = Color.Lerp(startColors[i], targetColor, t);
            }

            yield return null;
        }
    }

    private IEnumerator RestoreOriginalColors(float duration)
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
                enemyRenderers[i].material.color = Color.Lerp(startColors[i], originalColors[i], t);
            }

            yield return null;
        }
    }

    private void Explode()
    {
        if (isExploding) return;
        isExploding = true;

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
                    playerHealth.TakeDamage(damageAmount);
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

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Flash effect
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            Explode(); // Suicide bomber explodes when killed
        }
    }

    private IEnumerator DamageFlash()
    {
        // Get renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color[] originalColors = new Color[renderers.Length];

        // Store original colors and set to red
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
            renderers[i].material.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if hit by player laser
        if (other.CompareTag("PlayerLaser"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (cylinderTransform == null)
        {
            // Try to find in editor time
            GameObject levelObject = GameObject.FindGameObjectWithTag("Level");
            if (levelObject != null)
            {
                cylinderTransform = levelObject.transform;
            }
            else
            {
                return;
            }
        }

        // Calculate cylinder radius if not set
        if (cylinderRadius <= 0)
        {
            cylinderRadius = cylinderTransform.localScale.x * 0.5f;
        }

        // Calculate position on cylinder
        Vector3 cylinderPos = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            transform.position.y,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        // Calculate tangent
        Vector3 toCenter = cylinderTransform.position - cylinderPos;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        // Draw our position on cylinder
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(cylinderPos, 0.2f);
        Gizmos.DrawLine(transform.position, cylinderPos);

        // Draw our facing direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(cylinderPos, tangent * facingDirection * 2f);

        // Show detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cylinderPos, tangent * detectionRange * 0.5f);
        Gizmos.DrawRay(cylinderPos, -tangent * detectionRange * 0.5f);

        // Show explosion range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cylinderPos, explosionDistance);

        // Show explosion radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(cylinderPos, explosionRadius);

        // Draw height constraints
        Gizmos.color = Color.cyan;
        Vector3 minPos = new Vector3(cylinderPos.x, minHeight, cylinderPos.z);
        Vector3 maxPos = new Vector3(cylinderPos.x, maxHeight, cylinderPos.z);
        Gizmos.DrawLine(minPos, maxPos);
    }
}