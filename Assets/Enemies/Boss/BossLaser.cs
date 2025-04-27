using UnityEngine;

public class BossLaser : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    public float lifetime = 5f;
    public bool isPlayerProjectile = false;
    public GameObject impactEffectPrefab;
    public AudioClip impactSound;

    private Transform cylinderTransform;
    private float cylinderRadius;
    private Vector3 movementDirection;
    private float currentAngle;
    private float initialHeight;
    private float currentLifetime;
    private AudioSource audioSource;

    private void Awake()
    {
        // Add collider if missing
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.radius = 0.15f;
            collider.height = 1f;
            collider.direction = 2; // Z-axis oriented
        }

        // Add audio source if needed
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && impactSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        // Set up proper layer collision ignoring for Level
        int laserLayer = gameObject.layer;
        int levelLayer = LayerMask.NameToLayer("Level");

        if (levelLayer != -1) // Make sure the Level layer exists
        {
            Physics.IgnoreLayerCollision(laserLayer, levelLayer, true);
        }

        // Set tag for identification
        gameObject.tag = isPlayerProjectile ? "PlayerLaser" : "EnemyLaser";
    }

    public void Initialize(Transform cylinder, Vector3 direction, float projectileSpeed, int projectileDamage, bool fromPlayer)
    {
        cylinderTransform = cylinder;
        cylinderRadius = cylinder.localScale.x * 0.5f;
        movementDirection = direction.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        isPlayerProjectile = fromPlayer;
        currentLifetime = 0f;
        initialHeight = transform.position.y;

        // Calculate initial angle on cylinder
        if (cylinderTransform != null)
        {
            Vector3 toCenter = transform.position - cylinderTransform.position;
            toCenter.y = 0; // Ensure we're working in the XZ plane
            currentAngle = Mathf.Atan2(toCenter.x, toCenter.z);
        }
    }

    void Update()
    {
        currentLifetime += Time.deltaTime;
        if (currentLifetime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (cylinderTransform != null)
        {
            MoveAlongCylinder();
        }
        else
        {
            // Fallback to simple movement if no cylinder
            transform.position += movementDirection * speed * Time.deltaTime;
        }
    }

    void MoveAlongCylinder()
    {
        // Calculate movement in cylinder space
        float angleDelta = (speed * Time.deltaTime) / cylinderRadius;

        // Determine direction using dot product
        float directionSign = Mathf.Sign(Vector3.Dot(movementDirection,
                  Vector3.Cross(Vector3.up, cylinderTransform.position - transform.position)));

        currentAngle += angleDelta * directionSign;

        // Update position while staying at correct distance from cylinder
        Vector3 newPosition = cylinderTransform.position;
        newPosition.x += cylinderRadius * Mathf.Sin(currentAngle);
        newPosition.z += cylinderRadius * Mathf.Cos(currentAngle);
        newPosition.y = initialHeight; // Maintain original height

        transform.position = newPosition;

        // Rotate to face movement direction (tangent to cylinder)
        Vector3 tangent = new Vector3(Mathf.Cos(currentAngle), 0, -Mathf.Sin(currentAngle));
        if (directionSign < 0) tangent = -tangent;

        transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Skip collision with Level
        if (other.CompareTag("Level"))
        {
            return;
        }

        // Skip collision with Enemy objects if this is an enemy projectile
        if (!isPlayerProjectile && other.CompareTag("Enemy"))
        {
            return;
        }

        // Skip collision with Boss objects if this is an enemy projectile
        if (!isPlayerProjectile && other.CompareTag("Boss"))
        {
            return;
        }

        // Skip collision with self
        if ((isPlayerProjectile && other.CompareTag("Player")) ||
            (!isPlayerProjectile && (other.CompareTag("Enemy") || other.CompareTag("Boss"))))
        {
            return;
        }

        // Handle player hit
        if (other.CompareTag("Player") && !isPlayerProjectile)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }

        // Handle enemy hit
        if ((other.CompareTag("Enemy") || other.CompareTag("Boss")) && isPlayerProjectile)
        {
            // Try to get BossEnemy component first
            BossEnemy bossEnemy = other.GetComponent<BossEnemy>();
            if (bossEnemy != null)
            {
                bossEnemy.TakeDamage(damage);
            }
            else
            {
                // Try FlyingEnemy component
                FlyingEnemy flyingEnemy = other.GetComponent<FlyingEnemy>();
                if (flyingEnemy != null)
                {
                    flyingEnemy.TakeDamage(damage);
                }
                else
                {
                    // Try GroundEnemy component
                    GroundEnemy groundEnemy = other.GetComponent<GroundEnemy>();
                    if (groundEnemy != null)
                    {
                        groundEnemy.TakeDamage(damage);
                    }
                }
            }
        }

        // Create impact effect
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play impact sound
        if (audioSource != null && impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position, 1f);
        }

        Destroy(gameObject);
    }
}