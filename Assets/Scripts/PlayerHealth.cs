using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Configuration")]
    [SerializeField] private int maxHealth = 5;
    private int currentHealth;

    [Header("UI References")]
    [SerializeField] private GameObject[] heartImages; // Assign in Inspector
    [SerializeField] private bool findHeartsByTag = false; // Enable to find hearts by tag

    [Header("Damage Configuration")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    private bool isInvincible = false;

    [Header("Effects")]
    [SerializeField] private GameObject damageVFX;
    [SerializeField] private GameObject deathVFX;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;
    private AudioSource audioSource;

    [Header("Damage Sources")]
    [SerializeField] private string[] damageTags;

    private Renderer playerRenderer;
    private Color originalColor;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio source for 2D sound
        audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;

        if (findHeartsByTag)
        {
            FindHeartImagesByTag();
        }
    }

    // Rest of your code remains the same...
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthDisplay();
    }

    void FindHeartImagesByTag()
    {
        GameObject[] foundHearts = GameObject.FindGameObjectsWithTag("Heart");
        if (foundHearts.Length > 0)
        {
            heartImages = foundHearts;
            Debug.Log($"Found {heartImages.Length} hearts by tag");
        }
        else
        {
            Debug.LogWarning("No GameObjects with 'Heart' tag found!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        foreach (string tag in damageTags)
        {
            if (other.CompareTag(tag) && !isInvincible)
            {
                TakeDamage(1);
                break;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        UpdateHealthDisplay();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (damageSound) audioSource.PlayOneShot(damageSound);
            if (damageVFX) Instantiate(damageVFX, transform.position, Quaternion.identity);
            StartCoroutine(InvincibilityFrames());
        }
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        if (playerRenderer != null)
        {
            float flashSpeed = 0.1f;
            for (float t = 0; t < invincibilityDuration; t += flashSpeed)
            {
                playerRenderer.material.color = Color.red;
                yield return new WaitForSeconds(flashSpeed / 2);
                playerRenderer.material.color = originalColor;
                yield return new WaitForSeconds(flashSpeed / 2);
            }
        }
        else
        {
            yield return new WaitForSeconds(invincibilityDuration);
        }

        isInvincible = false;
    }

    private void UpdateHealthDisplay()
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            Debug.LogWarning("No heart images assigned!");
            return;
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                // Simply toggle the GameObject's active state
                heartImages[i].SetActive(i < currentHealth);
            }
        }
    }

    private void Die()
    {
        if (deathSound) audioSource.PlayOneShot(deathSound);
        if (deathVFX) Instantiate(deathVFX, transform.position, Quaternion.identity);

        Vector3 deathPosition = transform.position;
        gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied(deathPosition);
        }

        
        Object.FindFirstObjectByType<DeathMenuManager>().ShowDeathMenu();
    }

    public void RestoreHealth(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthDisplay();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthDisplay();
    }
}