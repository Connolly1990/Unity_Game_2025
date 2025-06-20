using UnityEngine;
using UnityEngine.UI;

public class ClonePowerup : MonoBehaviour
{
    [Header("Powerup Settings")]
    [Tooltip("The player GameObject reference")]
    public GameObject player;

    [Header("Clone Settings")]
    [Tooltip("Clone duration in seconds")]
    public float cloneDuration = 30f;
    [Tooltip("Offset position for clone spawn (relative to player)")]
    public Vector3 cloneSpawnOffset = new Vector3(0, 2f, 0);

    [Header("UI References")]
    [Tooltip("UI Text element to show progress")]
    public Text progressText;
    [Tooltip("UI Panel containing the progress text")]
    public GameObject progressPanel;

    [Header("Visual Effects")]
    [Tooltip("Rotation speed for the powerup")]
    public float rotationSpeed = 90f;
    [Tooltip("Despawn time if not collected")]
    public float despawnTime = 15f;

    [Header("Audio Settings")]
    [Tooltip("Sound to play when powerup is collected")]
    public AudioClip pickupSound;
    [Tooltip("Sound to play when clone quest is completed")]
    public AudioClip questCompleteSound;
    [Tooltip("Sound to play when part is collected for quest")]
    public AudioClip partCollectedSound;
    [Tooltip("Volume for sounds")]
    public float soundVolume = 1f;

    [Header("Collection Effects")]
    [Tooltip("Particle effect to spawn when collected")]
    public GameObject pickupEffect;
    [Tooltip("Particle effect to spawn when clone is created")]
    public GameObject cloneSpawnEffect;

    // Static instance for tracking clone quest
    public static ClonePowerup activeCloneQuest;

    // Quest tracking
    private int partsCollected = 0;
    private const int partsRequired = 3;
    private bool questActive = false;
    private bool isCollected = false;

    // Components
    private Collider powerupCollider;
    private Renderer powerupRenderer;

    private void Start()
    {
        // Get components
        powerupCollider = GetComponent<Collider>();
        powerupRenderer = GetComponent<Renderer>();

        // Ensure collider is trigger
        if (powerupCollider != null)
        {
            powerupCollider.isTrigger = true;
        }

        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        // Find UI elements if not assigned
        if (progressText == null)
        {
            progressText = GameObject.Find("CloneProgressText")?.GetComponent<Text>();
        }

        if (progressPanel == null)
        {
            progressPanel = GameObject.Find("CloneProgressPanel");
        }

        // Start despawn timer
        Invoke(nameof(DespawnPowerup), despawnTime);
    }

    private void Update()
    {
        if (isCollected) return;

        // Rotate the powerup
        if (rotationSpeed != 0f)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            CollectPowerup();
        }
    }

    private void CollectPowerup()
    {
        isCollected = true;

        // Set this as the active clone quest
        activeCloneQuest = this;

        // Play pickup sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
        }

        // Spawn pickup effect
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, transform.rotation);
            Destroy(effect, 3f);
        }

        // Start the quest
        StartCloneQuest();

        // Hide the powerup visual
        if (powerupRenderer != null)
        {
            powerupRenderer.enabled = false;
        }

        if (powerupCollider != null)
        {
            powerupCollider.enabled = false;
        }

        // Cancel despawn
        CancelInvoke(nameof(DespawnPowerup));
    }

    private void StartCloneQuest()
    {
        questActive = true;
        partsCollected = 0;

        // Show progress UI
        if (progressPanel != null)
        {
            progressPanel.SetActive(true);
        }

        UpdateProgressUI();

        Debug.Log("Clone quest started! Collect enemy parts to activate clone.");
    }

    public void OnPartCollected()
    {
        if (!questActive) return;

        partsCollected++;

        // Play part collected sound
        if (partCollectedSound != null && player != null)
        {
            AudioSource.PlayClipAtPoint(partCollectedSound, player.transform.position, soundVolume);
        }

        UpdateProgressUI();

        if (partsCollected >= partsRequired)
        {
            CompleteCloneQuest();
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"Kill and collect parts to unlock Clone. {partsCollected}/{partsRequired}";
        }
    }

    private void CompleteCloneQuest()
    {
        questActive = false;

        // Play quest complete sound
        if (questCompleteSound != null && player != null)
        {
            AudioSource.PlayClipAtPoint(questCompleteSound, player.transform.position, soundVolume);
        }

        // Hide progress UI
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }

        // Spawn clone
        SpawnClone();

        // Clear active quest
        activeCloneQuest = null;

        // Destroy this powerup
        Destroy(gameObject, 0.1f); // Small delay for sound to play
    }

    private void SpawnClone()
    {
        if (player == null) return;

        // Calculate spawn position
        Vector3 spawnPosition = player.transform.position + cloneSpawnOffset;

        // Spawn clone effect first
        if (cloneSpawnEffect != null)
        {
            GameObject spawnVFX = Instantiate(cloneSpawnEffect, spawnPosition, Quaternion.identity);
            Destroy(spawnVFX, 3f);
        }

        // Create clone GameObject
        GameObject clone = Instantiate(player, spawnPosition, player.transform.rotation);

        // Disable player-specific components on clone
        DisablePlayerComponents(clone);

        // Add clone controller
        PlayerClone cloneController = clone.AddComponent<PlayerClone>();
        cloneController.Initialize(player, cloneDuration);

        Debug.Log("Clone spawned and will last for " + cloneDuration + " seconds!");
    }

    private void DisablePlayerComponents(GameObject clone)
    {
        // Disable input and control components
        var playerMovement = clone.GetComponent<CylinderPlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Disable any other player-specific components that might exist
        // Add more component types here as needed for your specific player setup

        // Change tag to avoid conflicts
        clone.tag = "PlayerClone";

        // Optionally change material/color to distinguish clone
        var cloneRenderer = clone.GetComponent<Renderer>();
        if (cloneRenderer != null)
        {
            // You can change the material here to make it look different
            // cloneRenderer.material = cloneMaterial;
        }
    }

    private void DespawnPowerup()
    {
        if (!isCollected)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Clean up if this was the active quest
        if (activeCloneQuest == this)
        {
            activeCloneQuest = null;

            // Hide progress UI
            if (progressPanel != null)
            {
                progressPanel.SetActive(false);
            }
        }
    }
}