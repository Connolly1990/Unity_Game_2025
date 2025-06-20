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

    [Header("Audio Settings")]
    [Tooltip("Sound to play when collected")]
    public AudioClip collectSound;
    [Tooltip("Volume for collection sound")]
    public float soundVolume = 1f;

    [Header("Collection Effects")]
    [Tooltip("Particle effect to spawn on collection")]
    public GameObject collectionEffect;
    [Tooltip("Duration to wait before destroying after collection (for effects)")]
    public float destroyDelay = 0.1f;

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

        // Notify clone quest if active
        if (ClonePowerup.activeCloneQuest != null)
        {
            ClonePowerup.activeCloneQuest.OnPartCollected();
        }

        // Play collection sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }

        // Spawn collection effect
        if (collectionEffect != null)
        {
            GameObject effect = Instantiate(collectionEffect, transform.position, transform.rotation);
            // Auto-destroy effect after 2 seconds if it doesn't destroy itself
            Destroy(effect, 2f);
        }

        // Hide visual components immediately
        if (objectRenderer != null)
        {
            objectRenderer.enabled = false;
        }
        if (objectCollider != null)
        {
            objectCollider.enabled = false;
        }

        // Destroy the collectible after a short delay (for sound/effects to play)
        Destroy(gameObject, destroyDelay);
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