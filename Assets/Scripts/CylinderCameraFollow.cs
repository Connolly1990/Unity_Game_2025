using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CylinderCameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform cylinder;

    [Header("Position Settings")]
    [Range(5f, 30f)] public float distanceFromSurface = 12f;
    [Range(0f, 10f)] public float fixedHeight = 5.5f;
    [Range(0f, 10f)] public float lookAheadDistance = 3.5f;

    [Header("Visual Settings")]
    [Range(40f, 80f)] public float fieldOfView = 60f;
    [Range(0f, 1f)] public float ambientIntensity = 0.35f;
    public Color ambientColor = new Color(0.2f, 0.3f, 0.4f);

    private float cylinderRadius;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cylinderRadius = cylinder.localScale.x * 0.5f;

        ApplyVisualSettings();
        UpdateCamera(true); // Snap immediately
    }

    void LateUpdate()
    {
        UpdateCamera(false);
    }

    void UpdateCamera(bool snap)
    {
        // 1. Get direction FROM cylinder TO player (flipped from previous)
        Vector3 cylinderToPlayer = (player.position - cylinder.position).normalized;

        // 2. Camera position (SAME SIDE as player, outside cylinder)
        Vector3 targetPos = cylinder.position +
                          (cylinderToPlayer * (cylinderRadius + distanceFromSurface)) +
                          (Vector3.up * fixedHeight);

        transform.position = snap ? targetPos :
            Vector3.Lerp(transform.position, targetPos, 5f * Time.deltaTime);

        // Clamp the camera's Y position so it doesn't go under the ground
        Vector3 pos = transform.position;
        pos.y = Mathf.Max(pos.y, 1f); // 1f is your minimum height. Adjust if needed.
        transform.position = pos;


        // 3. Look target (slightly into the cylinder)
        Vector3 lookTarget = cylinder.position -
                           (cylinderToPlayer * lookAheadDistance) +
                           (Vector3.up * fixedHeight * 0.4f);

        // 4. Locked arcade rotation
        transform.rotation = Quaternion.LookRotation(
            (lookTarget - transform.position).normalized,
            Vector3.up
        );
    }

    void ApplyVisualSettings()
    {
        cam.fieldOfView = fieldOfView;
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.ambientLight = ambientColor;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        ApplyVisualSettings();
    }
#endif
}