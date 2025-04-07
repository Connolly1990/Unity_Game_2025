using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
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
    private Rigidbody rb;
    private float currentLifetime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.2f;
        }
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

        // Calculate initial angle on cylinder
        Vector3 toCenter = transform.position - cylinderTransform.position;
        toCenter.y = 0; // Ensure we're working in the XZ plane
        currentAngle = Mathf.Atan2(toCenter.x, toCenter.z);
        initialHeight = transform.position.y;

        // Set initial rotation to match movement direction
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        float directionSign = Mathf.Sign(Vector3.Dot(movementDirection, tangent));
        transform.rotation = Quaternion.LookRotation(tangent * directionSign, Vector3.up);

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        currentLifetime += Time.fixedDeltaTime;
        MoveAlongCylinder();
    }

    private void MoveAlongCylinder()
    {
        // Calculate the tangent direction at current position
        Vector3 toCenter = transform.position - cylinderTransform.position;
        toCenter.y = 0; // Ensure we're working in the XZ plane
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        // Determine direction along tangent based on initial movement direction
        float directionSign = Mathf.Sign(Vector3.Dot(movementDirection, tangent));

        // Calculate angle change based on speed
        float angleDelta = (speed * Time.fixedDeltaTime) / cylinderRadius;
        currentAngle += angleDelta * directionSign;

        // Calculate new position on cylinder surface
        Vector3 newPosition = cylinderTransform.position + new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            initialHeight,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        // Update tangent direction for new position
        Vector3 newToCenter = newPosition - cylinderTransform.position;
        newToCenter.y = 0;
        Vector3 newTangent = Vector3.Cross(newToCenter.normalized, Vector3.up);

        // Use physics to move (prevents teleporting)
        rb.MovePosition(newPosition);
        rb.MoveRotation(Quaternion.LookRotation(newTangent * directionSign, Vector3.up));
    }

    private void OnTriggerEnter(Collider other)
    {
        // Skip collision with self or same type
        if ((isPlayerProjectile && other.CompareTag("Player")) ||
            (!isPlayerProjectile && other.CompareTag("Enemy")))
        {
            return;
        }

        // Handle player hit
        if (!isPlayerProjectile && other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(damage);
        }

        // Handle enemy hit
        if (isPlayerProjectile && other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(damage);
        }

        // Create impact effect
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}