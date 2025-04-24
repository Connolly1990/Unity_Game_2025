using UnityEngine;

public class PotResetOnMiss : MonoBehaviour
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        // Store the original spawn position and rotation
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the pot hits a "miss" collider
        if (other.CompareTag("Miss"))
        {
            ResetPot();
        }
    }

    void ResetPot()
    {
        // Move the Pot back to its original position and rotation
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }
}
