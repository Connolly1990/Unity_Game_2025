using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SuicideBomber : MonoBehaviour
{
    [Header("References")]
    public Transform enemyModel;
    public Transform cylinderTransform; // Reference to the cylinder
    public Transform middleGuideline;   // Reference to the guideline
    public AudioClip explosionSound;    // Audio clip for explosion (drag in inspector)
    public AudioClip damageSound;      // Audio clip for when taking damage (drag in inspector)

    [Header("Basic Stats")]
    public int maxHealth = 2;
    public int damageAmount = 2; // Higher damage since it's a suicide attack

    [Header("Movement Settings")]
    public float moveSpeed = 5f;             // Regular patrol speed
    public float chargeSpeed = 7f;           // Speed when charging at player
    public float detectionRange = 15f;       // Detection angle in degrees
    public float explosionDistance = 2f;     // Distance at which it explodes
    public float explosionDelay = 1.5f;      // Time before explosion

    [Header("Explosion Settings")]
    public float blinkFrequency = 0.5f;
    public float explosionRadius = 5f;
    public float fadeDuration = 0.2f;
    public GameObject explosionEffectPrefab;

    // Private variables
    private Transform playerTransform;
    private float cylinderRadius;
    private float currentAngle;
    private Renderer[] enemyRenderers;
    private Color[] originalColors;
    private Rigidbody rb;
    private int currentHealth;
    private bool isBlinking = false;
    private bool movingLeft = true;         // Direction along the guideline
    private bool chargingAtPlayer = false;  // Whether actively pursuing the player
    private bool isExploding = false;       // Whether in explosion sequence
    private float explosionTimer = 0f;      // Countdown to explosion
    private AudioSource audioSource;

    private void Awake()
    {
        // Setup rigidbody
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        // Find references if not set
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (cylinderTransform == null)
            cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;

        if (middleGuideline == null && cylinderTransform != null)
        {
            // Try to find guideline as child of cylinder
            middleGuideline = cylinderTransform.Find("MiddleGuideline");
        }

        // Initialize health
        currentHealth = maxHealth;

        // Setup renderers
        enemyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            originalColors[i] = enemyRenderers[i].material.color;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio source for 2D sound
        audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Start()
    {
        // Double check references
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

            // Initialize position on middle rail if available
            if (middleGuideline != null)
            {
                transform.position = new Vector3(transform.position.x, middleGuideline.position.y, transform.position.z);
            }

            // Initialize random starting angle
            currentAngle = Random.Range(0f, 2f * Mathf.PI);
            UpdatePositionAndRotation(true);
        }

        // Start blinking effect
        StartCoroutine(BlinkRoutine());
    }

    private void FixedUpdate()
    {
        if (playerTransform == null || cylinderTransform == null) return;

        if (isExploding)
        {
            HandleExplosion();
            // Make sure position is maintained even during explosion
            UpdatePositionAndRotation();
            return;
        }

        if (playerTransform != null)
        {
            DetectPlayer();
        }

        MoveEnemy();
        UpdatePositionAndRotation();
    }

    void DetectPlayer()
    {
        // Get player angle on cylinder
        float playerAngle = Mathf.Atan2(
            playerTransform.position.z - cylinderTransform.position.z,
            playerTransform.position.x - cylinderTransform.position.x
        );

        // Normalize angles for comparison
        float wrappedCurrentAngle = currentAngle % (2f * Mathf.PI);
        if (wrappedCurrentAngle < 0) wrappedCurrentAngle += 2f * Mathf.PI;

        float wrappedPlayerAngle = playerAngle % (2f * Mathf.PI);
        if (wrappedPlayerAngle < 0) wrappedPlayerAngle += 2f * Mathf.PI;

        // Calculate angle difference in degrees
        float angleDifference = Mathf.Abs(wrappedCurrentAngle - wrappedPlayerAngle) * Mathf.Rad2Deg;
        if (angleDifference > 180f)
            angleDifference = 360f - angleDifference;

        // Calculate distance to player for explosion check
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Check if close enough to explode
        if (distanceToPlayer <= explosionDistance)
        {
            StartExplosion();
            return;
        }

        // If charging at player, continuously update direction
        if (chargingAtPlayer)
        {
            // Determine if player is to the left or right
            float clockwiseDifference = (wrappedPlayerAngle - wrappedCurrentAngle + 2f * Mathf.PI) % (2f * Mathf.PI);
            movingLeft = clockwiseDifference < Mathf.PI;
            return;
        }

        // If player is within detection range
        if (angleDifference < detectionRange)
        {
            // Start charging at player
            chargingAtPlayer = true;

            // Visual indication that bomber detected player
            foreach (Renderer renderer in enemyRenderers)
            {
                renderer.material.color = Color.red;
            }
        }
    }

    void MoveEnemy()
    {
        float speed = chargingAtPlayer ? chargeSpeed : moveSpeed;

        // Move along the rail
        if (movingLeft)
        {
            currentAngle += speed * Time.fixedDeltaTime / cylinderRadius;
        }
        else
        {
            currentAngle -= speed * Time.fixedDeltaTime / cylinderRadius;
        }

        // Normalize angle to prevent overflow
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);
    }

    void UpdatePositionAndRotation(bool snap = false)
    {
        if (cylinderTransform == null) return;

        // Calculate position on cylinder (relative to cylinder position)
        float x = cylinderTransform.position.x + cylinderRadius * Mathf.Cos(currentAngle);
        float z = cylinderTransform.position.z + cylinderRadius * Mathf.Sin(currentAngle);
        float y = middleGuideline ? middleGuideline.position.y : transform.position.y;
        Vector3 targetPosition = new Vector3(x, y, z);

        // Update the enemy's position on the cylinder
        if (snap)
        {
            transform.position = targetPosition;
        }
        else
        {
            // Use rigidbody for movement but ensure position is correct
            rb.MovePosition(targetPosition);

            // Additionally set the transform position to ensure it stays on the cylinder
            // This is the key fix to prevent detachment
            transform.position = targetPosition;
        }

        // Make enemy face tangent to the cylinder (direction of movement)
        Vector3 tangent = new Vector3(-Mathf.Sin(currentAngle), 0f, Mathf.Cos(currentAngle));
        if (movingLeft)
            tangent = -tangent;

        transform.forward = tangent;
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

    void StartExplosion()
    {
        isExploding = true;
        explosionTimer = explosionDelay;

        // Stop all movement but ensure we stay on cylinder
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Make sure we're positioned correctly
        UpdatePositionAndRotation(true);

        // Visual indication that bomber is about to explode
        foreach (Renderer renderer in enemyRenderers)
        {
            renderer.material.color = Color.yellow;
        }
    }

    void HandleExplosion()
    {
        explosionTimer -= Time.fixedDeltaTime;

        // Flash effect as countdown progresses
        if ((explosionTimer * 4f) % 0.5f < 0.25f)
        {
            foreach (Renderer renderer in enemyRenderers)
            {
                renderer.material.color = Color.yellow;
            }
        }
        else
        {
            foreach (Renderer renderer in enemyRenderers)
            {
                renderer.material.color = Color.red;
            }
        }

        if (explosionTimer <= 0)
        {
            Explode();
        }
    }

    void Explode()
    {
        // Play explosion sound as 2D
        if (explosionSound != null)
        {
            // Create a temporary AudioSource for 2D death sound
            GameObject tempAudioObject = new GameObject("TempAudio");
            AudioSource tempAudio = tempAudioObject.AddComponent<AudioSource>();
            tempAudio.spatialBlend = 0f; // 2D sound
            tempAudio.PlayOneShot(explosionSound);
            Destroy(tempAudioObject, explosionSound.length);
        }

        // Create explosion effect
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            // Fallback explosion effect if no prefab is assigned
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

            // Destroy explosion after effect completes
            Destroy(explosion, 2f);
        }

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

            // Apply explosion force to rigidbodies (except the bomber itself)
            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null && hitRb != rb)
            {
                hitRb.AddExplosionForce(explosionRadius * 2, transform.position, explosionRadius);
            }
        }

        // Destroy the bomber
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

        // Play damage sound
        if (damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        // Flash effect
        StartCoroutine(DamageFlash());

        // Force position update to stay on cylinder when hit
        UpdatePositionAndRotation(true);

        if (currentHealth <= 0)
        {
            // Immediately explode when health is depleted instead of starting the countdown
            Explode();
        }
    }

    private IEnumerator DamageFlash()
    {
        // Store original colors and set to red
        Color[] tempColors = new Color[enemyRenderers.Length];
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            tempColors[i] = enemyRenderers[i].material.color;
            enemyRenderers[i].material.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        // Restore colors
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            if (enemyRenderers[i] != null)
            {
                enemyRenderers[i].material.color = tempColors[i];
            }
        }

        // Make sure we're still on the cylinder after the flash effect
        UpdatePositionAndRotation(true);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if hit by player laser
        if (other.CompareTag("PlayerLaser"))
        {
            TakeDamage(1);
            Destroy(other.gameObject);

            // Force position update immediately after laser hit
            UpdatePositionAndRotation(true);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // If collided with player, start explosion sequence
        if (collision.gameObject.CompareTag("Player") && !isExploding)
        {
            StartExplosion();
        }

        // Force position update on any collision
        UpdatePositionAndRotation(true);
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
                cylinderRadius = cylinderTransform.localScale.x * 0.5f;
            }
            else
            {
                return;
            }
        }

        // Calculate position on cylinder
        Vector3 cylinderPos = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Cos(currentAngle),
            transform.position.y,
            cylinderRadius * Mathf.Sin(currentAngle)
        );

        // Calculate tangent
        Vector3 tangent = new Vector3(-Mathf.Sin(currentAngle), 0f, Mathf.Cos(currentAngle));
        if (movingLeft)
            tangent = -tangent;

        // Draw our position on cylinder
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(cylinderPos, 0.2f);
        Gizmos.DrawLine(transform.position, cylinderPos);

        // Draw our facing direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(cylinderPos, tangent * 2f);

        // Show detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cylinderPos, detectionRange);

        // Show explosion distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cylinderPos, explosionDistance);

        // Show explosion radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(cylinderPos, explosionRadius);
    }
}