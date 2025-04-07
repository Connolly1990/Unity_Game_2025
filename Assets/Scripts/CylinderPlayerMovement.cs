using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CylinderPlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cylinderTransform;
    public Transform playerModel;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;
    public float boundaryOffset = 0.5f;

    public bool CanShoot { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;
    public int CurrentDirection { get; private set; } = 0; // 0=idle, 1=forward, -1=backward

    private float currentAngle = 0f;
    private float cylinderRadius;
    private Rigidbody rb;
    private int horizontalDirection = 0;
    private int verticalDirection = 0;
    private bool canMoveUp = true;
    private bool canMoveDown = true;
    private bool hasMovedSinceStart = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        cylinderRadius = cylinderTransform.localScale.x * 0.5f;
        currentAngle = Mathf.Atan2(transform.position.x - cylinderTransform.position.x,
                                 transform.position.z - cylinderTransform.position.z);
        UpdatePositionAndRotation(transform.position.y, true);
        CanShoot = false;
    }

    void Update() => HandleInput();

    void FixedUpdate()
    {
        IsMoving = horizontalDirection != 0 || verticalDirection != 0;

        if (IsMoving && !hasMovedSinceStart)
        {
            hasMovedSinceStart = true;
            CanShoot = true;
        }

        // Update raw direction based on input
        int rawDirection = (int)Mathf.Sign(horizontalDirection);

        // Update position using angle (normalized to prevent overflow)
        currentAngle += horizontalDirection * moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Calculate actual direction relative to cylinder tangent
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        // This will ensure CurrentDirection is relative to the player's orientation on the cylinder
        if (rawDirection != 0)
            CurrentDirection = rawDirection;

        // Vertical movement
        float newY = rb.position.y;
        if (verticalDirection > 0 && canMoveUp)
            newY += moveSpeed * Time.fixedDeltaTime;
        else if (verticalDirection < 0 && canMoveDown)
            newY -= moveSpeed * Time.fixedDeltaTime;

        UpdatePositionAndRotation(newY);
        UpdateShipOrientation();
    }

    void HandleInput()
    {
        // Horizontal input (toggle-based)
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            horizontalDirection = horizontalDirection == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            horizontalDirection = horizontalDirection == -1 ? 0 : -1;

        // Vertical input (toggle-based)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            verticalDirection = verticalDirection == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            verticalDirection = verticalDirection == -1 ? 0 : -1;
    }

    void UpdatePositionAndRotation(float newY, bool snap = false)
    {
        Vector3 targetPosition = new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            newY,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        Vector3 toCenter = cylinderTransform.position - targetPosition;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(tangent * CurrentDirection, Vector3.up)
                                 * Quaternion.Euler(270, 0, 0);

        if (snap)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            rb.MovePosition(targetPosition);
            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation,
                           rotationSpeed * Time.fixedDeltaTime));
        }
    }

    void UpdateShipOrientation()
    {
        if (CurrentDirection == 0) return;

        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        playerModel.rotation = Quaternion.LookRotation(tangent * CurrentDirection, Vector3.up)
                             * Quaternion.Euler(270, 0, 0);
    }

    void OnDrawGizmos()
    {
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, tangent * CurrentDirection * 2f);
    }
}
