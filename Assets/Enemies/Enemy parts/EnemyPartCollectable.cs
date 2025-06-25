using UnityEngine;

public class EnemyPartCollectible : MonoBehaviour
{
    [Header("Score Settings")]
    public int scoreValue = 200;
    public float despawnTime = 6f;

    [Header("Visual Effects")]
    public float rotationSpeed = 90f;
    public float blinkFrequency = 2f;
    public Vector3 rotationAxis = Vector3.up;

    [Header("Audio Settings")]
    public AudioClip collectSound;
    public float soundVolume = 1f;

    [Header("Collection Effects")]
    public GameObject collectionEffect;
    public float destroyDelay = 0.1f;

    private Renderer objectRenderer;
    private Collider objectCollider;
    private bool isCollected = false;
    private float blinkTimer = 0f;
    private bool isVisible = true;

    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        objectCollider = GetComponent<Collider>();

        if (objectCollider != null)
        {
            objectCollider.isTrigger = true;
        }

        StartCoroutine(DespawnAfterTime());
        rotationAxis = rotationAxis.normalized;
    }

    private void Update()
    {
        if (isCollected) return;

        if (rotationSpeed != 0f)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
        }

        HandleBlinking();
    }

    private void HandleBlinking()
    {
        if (objectRenderer == null || blinkFrequency <= 0f) return;

        blinkTimer += Time.deltaTime;
        float blinkInterval = 1f / blinkFrequency;

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
        if (other.CompareTag("Player") && !isCollected)
        {
            CollectPart();
        }
    }

    private void CollectPart()
    {
        isCollected = true;

        // Always add score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        // Only count towards quest if there's an active nuke quest
        if (LaserNukePowerup.activeNukeQuest != null)
        {
            LaserNukePowerup.activeNukeQuest.OnPartCollected();
        }

        // Play collection effects
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }

        if (collectionEffect != null)
        {
            GameObject effect = Instantiate(collectionEffect, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }

        // Hide the object
        if (objectRenderer != null)
        {
            objectRenderer.enabled = false;
        }

        if (objectCollider != null)
        {
            objectCollider.enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }

    private System.Collections.IEnumerator DespawnAfterTime()
    {
        yield return new WaitForSeconds(despawnTime);

        if (!isCollected)
        {
            Destroy(gameObject);
        }
    }
}