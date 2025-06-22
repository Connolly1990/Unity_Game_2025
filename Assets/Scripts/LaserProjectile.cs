using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 20f;
    public float lifetime = 2f;

    [Header("Nuke Settings")]
    public bool isNukeLaser = false; // Checkbox to mark if this is a nuke laser

    private Transform cylinderTransform;
    private float cylinderRadius;
    private Vector3 movementDirection;
    private float currentLifetime;
    private float currentAngle;
    private float initialHeight;
    private bool hasHit = false;

    public void Initialize(Transform cylinder, Vector3 direction)
    {
        cylinderTransform = cylinder;
        cylinderRadius = cylinder.localScale.x * 0.5f;
        movementDirection = direction.normalized;
        currentLifetime = 0f;
        initialHeight = transform.position.y;
        hasHit = false;

        // Calculate initial angle on cylinder
        Vector3 toCenter = transform.position - cylinderTransform.position;
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
        // Only destroy regular lasers on impact, not nuke lasers
        if (!isNukeLaser && !hasHit)
        {
            if (other.CompareTag("Enemy") || other.CompareTag("Wall") || other.CompareTag("Obstacle"))
            {
                hasHit = true;
                Destroy(gameObject);
            }
        }

        // Nuke lasers continue through everything - their damage is handled by LaserNukeDamage component
    }
}