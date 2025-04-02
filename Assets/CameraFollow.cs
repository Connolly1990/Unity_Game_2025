using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;  // The player's transform
    public Vector3 offset;  // Offset of the camera from the player
    public float smoothSpeed = 0.125f;  // Speed of camera smoothing

    private void LateUpdate()
    {
        // Desired position is the player's position + the offset
        Vector3 desiredPosition = player.position + offset;

        // Smoothly interpolate between the camera's current position and the desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Set the camera's position
        transform.position = smoothedPosition;
    }
}
