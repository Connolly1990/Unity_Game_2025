using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public float invulnerabilityDuration = 1f;
    public Image healthBar;
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.1f;

    // Optional death effects
    public bool useDeathParticles = true;
    public Color deathParticleColor = Color.red;
    public int particleCount = 20;
    public float particleForce = 5f;

    [SerializeField] private MonoBehaviour gameManagerObject;

    private int currentHealth;
    private bool isInvulnerable;
    private Renderer[] renderers;
    private Color[] originalColors;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        // Cache renderers for flash effect
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(propertyBlock);
            originalColors[i] = propertyBlock.GetColor(ColorProperty);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable) return;

        currentHealth -= Mathf.RoundToInt(damage);

        // Use non-coroutine methods
        _ = FlashEffect();
        _ = SetInvulnerability();

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    private async Task FlashEffect()
    {
        // Flash to damage color using MaterialPropertyBlock
        for (int i = 0; i < renderers.Length; i++)
        {
            propertyBlock.SetColor(ColorProperty, damageFlashColor);
            renderers[i].SetPropertyBlock(propertyBlock);
        }

        await Task.Delay(Mathf.RoundToInt(flashDuration * 1000));

        // Reset to original color
        for (int i = 0; i < renderers.Length; i++)
        {
            propertyBlock.SetColor(ColorProperty, originalColors[i]);
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    private async Task SetInvulnerability()
    {
        isInvulnerable = true;
        await Task.Delay(Mathf.RoundToInt(invulnerabilityDuration * 1000));
        isInvulnerable = false;
    }

    private void Die()
    {
        // Create simple particle effect since no explosion prefab exists
        if (useDeathParticles)
        {
            CreateDeathParticles();
        }

        // Disable player components instead of destroying
        var playerMovement = GetComponent<CylinderPlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Handle game over logic using reflection to avoid direct dependency
        if (gameManagerObject != null)
        {
            // Try to call GameOver method via reflection
            var gameOverMethod = gameManagerObject.GetType().GetMethod("GameOver");
            if (gameOverMethod != null)
            {
                gameOverMethod.Invoke(gameManagerObject, null);
            }
            else
            {
                Debug.LogWarning("GameOver method not found on assigned gameManagerObject");
                _ = DelayedRestartLevel(3f);
            }
        }
        else
        {
            // Fallback if no game manager
            _ = DelayedRestartLevel(3f);
        }

        // Hide player model
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Disable colliders
        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    private void CreateDeathParticles()
    {
        // Create a simple particle effect using primitive game objects
        for (int i = 0; i < particleCount; i++)
        {
            // Create a small cube as a "particle"
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            particle.transform.localScale = Vector3.one * 0.2f;
            particle.transform.position = transform.position;

            // Add material with death color
            var renderer = particle.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = deathParticleColor;

            // Add rigidbody for physics
            var rb = particle.AddComponent<Rigidbody>();
            rb.AddExplosionForce(particleForce, transform.position, 2f);

            // Destroy after delay
            Destroy(particle, 2f);
        }
    }

    private async Task DelayedRestartLevel(float delay)
    {
        await Task.Delay(Mathf.RoundToInt(delay * 1000));
        RestartLevel();
    }

    private void RestartLevel()
    {
        // Proper scene loading with loading screen option
        var currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentScene);
    }
}