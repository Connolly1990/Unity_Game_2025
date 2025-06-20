using UnityEngine;
using System.Collections;

public class EnemyPartCollectible : MonoBehaviour
{
    [Header("Score Settings")]
    [Tooltip("Points awarded when this part is collected")]
    public int scoreValue = 200;

    [Header("Despawn Settings")]
    [Tooltip("Time in seconds before the part despawns")]
    public float despawnTime = 6f;

    [Header("Visual Effects")]
    [Tooltip("Rotation speed (degrees per second)")]
    public float rotationSpeed = 90f;

    [Tooltip("Blink frequency (blinks per second)")]
    public float blinkFrequency = 2f;

    [Tooltip("Axis to rotate around (normalized)")]
    public Vector3 rotationAxis = Vector3.up;

    private Renderer objectRenderer;
    private Collider objectCollider;
    private bool isCollected = false;
    private float blinkTimer = 0f;
    private bool isVisible = true;

    private void Start()
    {
        // Get components
        objectRenderer = GetComponent<Renderer>();
        objectCollider = GetComponent<Collider>();

        // Ensure the collider is set as trigger
        if (objectCollider != null)
        {
            objectCollider.isTrigger = true;
        }

        // Start the despawn timer
        StartCoroutine(DespawnAfterTime());

        // Normalize rotation axis
        rotationAxis = rotationAxis.normalized;
    }

    private void Update()
    {
        if (isCollected) return;

        // Rotate the object
        if (rotationSpeed != 0f)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
        }

        // Handle blinking effect
        HandleBlinking();
    }

    private void HandleBlinking()
    {
        if (objectRenderer == null || blinkFrequency <= 0f) return;

        blinkTimer += Time.deltaTime;
        float blinkInterval = 1f / blinkFrequency;

        // Toggle visibility based on blink frequency
        if (blinkTimer >= blinkInterval / 2f)
        {
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                isVisible = true;
            }
            else
            {
                isVisible = false;
            }

            objectRenderer.enabled = isVisible;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object has the "Player" tag
        if (other.CompareTag("Player") && !isCollected)
        {
            CollectPart();
        }
    }

    private void CollectPart()
    {
        // Prevent multiple collections
        isCollected = true;

        // Add score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        // Optional: Play collection sound or effect here
        // AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // Destroy the collectible
        Destroy(gameObject);
    }

    private IEnumerator DespawnAfterTime()
    {
        yield return new WaitForSeconds(despawnTime);

        // Only despawn if not already collected
        if (!isCollected)
        {
            Destroy(gameObject);
        }
    }

    // Optional: Visual feedback when player is nearby
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            // You could add additional visual feedback here
            // like pulsing the scale or changing color
        }
    }
}