using UnityEngine;

public class EnemyLaser : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    public float lifetime = 5f;
    public bool isPlayerProjectile = false;
    public GameObject impactEffectPrefab;

    private Transform cylinderTransform;
    private float cylinderRadius;
    private Vector3 movementDirection;
    private float currentAngle;
    private float initialHeight;
    private float currentLifetime;

    private void Awake()
    {
        // Add collider if missing
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.radius = 0.1f;
            collider.height = 1f;
            collider.direction = 2; // Z-axis oriented
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
        Vector3 toCenter = transform.position - cylinderTransform.position;
        toCenter.y = 0; // Ensure we're working in the XZ plane
        currentAngle = Mathf.Atan2(toCenter.x, toCenter.z);
    }

    void Update()
    {
        currentLifetime += Time.deltaTime;
        if (currentLifetime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        MoveAlongCylinder();
    }

    void MoveAlongCylinder()
    {
        // Calculate movement in cylinder space
        float angleDelta = (speed * Time.deltaTime) / cylinderRadius;

        // Use the dot product to determine the correct sign
        float directionSign = Mathf.Sign(Vector3.Dot(movementDirection,
                      Vector3.Cross(Vector3.up, cylinderTransform.position - transform.position)));

        currentAngle += angleDelta * directionSign;

        // Update position while staying on cylinder
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

        // Skip collision with Enemy objects
        if (other.CompareTag("Enemy"))
        {
            return;
        }

        // Skip collision with self
        if ((isPlayerProjectile && other.CompareTag("Player")))
        {
            return;
        }

        // Handle player hit
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Enemy Laser hit Player");
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(damage);
        }

        // Create impact effect
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}