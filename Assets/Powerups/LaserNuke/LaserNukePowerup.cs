using UnityEngine;
using TMPro;

public class LaserNukePowerup : MonoBehaviour
{
    public static LaserNukePowerup activeNukeQuest;

    [Header("Nuke Quest Settings")]
    public int partsNeeded = 5;
    private int currentParts = 0;
    private bool isComplete = false;
    private bool questStarted = false; // Track if quest has been started

    [Header("Laser Nuke Settings")]
    public GameObject laserNukePrefab;
    private Transform firePoint;
    private CylinderPlayerMovement playerMovement;

    [Header("UI Settings")]
    private TextMeshProUGUI progressText;
    private GameObject progressUI;

    [Header("Audio Settings")]
    public AudioClip fullChargeSound;
    public AudioClip nukeLaunchSound;
    public float soundVolume = 1f;

    private void Start()
    {
        // Find required components by tags
        FindRequiredComponents();

        // Register this instance globally
        activeNukeQuest = this;
        currentParts = 0;
        isComplete = false;
        questStarted = false;

        // Initially hide the UI until quest starts
        if (progressUI != null)
        {
            progressUI.SetActive(false);
        }
    }

    private void FindRequiredComponents()
    {
        // Find fire point by tag
        GameObject firePointObj = GameObject.FindGameObjectWithTag("FirePoint");
        if (firePointObj != null)
        {
            firePoint = firePointObj.transform;
        }
        else
        {
            Debug.LogError("LaserNukePowerup: No GameObject with tag 'FirePoint' found!");
        }

        // Find player movement component
        playerMovement = Object.FindFirstObjectByType<CylinderPlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("LaserNukePowerup: No CylinderPlayerMovement found in scene!");
        }

        // Find UI elements by tag
        GameObject objectiveUI = GameObject.FindGameObjectWithTag("Objective1");
        if (objectiveUI != null)
        {
            progressUI = objectiveUI;
            progressText = objectiveUI.GetComponent<TextMeshProUGUI>();
            if (progressText == null)
            {
                progressText = objectiveUI.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (progressText == null)
            {
                Debug.LogError("LaserNukePowerup: No TextMeshProUGUI component found on 'Objective1' GameObject or its children!");
            }
        }
        else
        {
            Debug.LogError("LaserNukePowerup: No GameObject with tag 'Objective1' found!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartNukeQuest();
            // Hide the powerup pickup but don't destroy it yet - we need it to track the quest
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
        }
    }

    private void StartNukeQuest()
    {
        questStarted = true;
        Debug.Log("Laser Nuke quest started! Collect enemy parts to charge the nuke.");
        UpdateUI();
    }

    public void OnPartCollected()
    {
        if (isComplete) return;

        // Only count parts if the quest is active (this object exists)
        if (activeNukeQuest == null) return;

        currentParts++;
        UpdateUI();

        if (currentParts >= partsNeeded)
        {
            CompleteNukeQuest();
        }
    }

    private void CompleteNukeQuest()
    {
        isComplete = true;

        if (fullChargeSound != null)
        {
            AudioSource.PlayClipAtPoint(fullChargeSound, transform.position, soundVolume);
        }

        Debug.Log("Laser Nuke is fully charged!");
        UpdateUI();

        // Fire the nuke laser immediately
        FireNukeLaser();

        // Hide UI and clean up after firing
        if (progressUI != null)
        {
            Invoke("CleanupQuest", 1f);
        }
        else
        {
            CleanupQuest();
        }
    }

    private void CleanupQuest()
    {
        if (progressUI != null)
        {
            progressUI.SetActive(false);
        }

        // Clear the static reference and destroy this quest object
        activeNukeQuest = null;
        Destroy(gameObject);
    }

    private void FireNukeLaser()
    {
        if (laserNukePrefab == null || firePoint == null || playerMovement == null) return;

        // Play nuke launch sound
        if (nukeLaunchSound != null)
        {
            AudioSource.PlayClipAtPoint(nukeLaunchSound, firePoint.position, soundVolume);
        }

        // Calculate tangent direction using cylinder math (same as normal laser)
        Vector3 toCenter = firePoint.position - playerMovement.cylinderTransform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        // Fire nuke in both directions
        FireNukeInDirection(tangent);   // One direction
        FireNukeInDirection(-tangent);  // Opposite direction

        Debug.Log("Laser Nukes fired in both directions!");
    }

    private void FireNukeInDirection(Vector3 direction)
    {
        // Instantiate the nuke laser
        GameObject nukeLaser = Instantiate(laserNukePrefab, firePoint.position,
                                        Quaternion.LookRotation(direction, Vector3.up));

        // Initialize the nuke laser
        var nukeComponent = nukeLaser.GetComponent<LaserProjectile>();
        if (nukeComponent != null)
        {
            nukeComponent.Initialize(playerMovement.cylinderTransform, direction);
        }
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            if (!isComplete)
            {
                progressText.text = $"Nuke Parts: {currentParts}/{partsNeeded}";
            }
            else
            {
                progressText.text = "NUKE READY!";
            }
        }

        // Show UI immediately when quest starts, regardless of parts collected
        if (progressUI != null && questStarted)
        {
            progressUI.SetActive(true);
        }
    }

    private void HideUI()
    {
        if (progressUI != null)
        {
            progressUI.SetActive(false);
        }
    }

    public void ResetNukeQuest()
    {
        currentParts = 0;
        isComplete = false;
        questStarted = false;
        UpdateUI();
    }

    public int GetPartsCollected()
    {
        return currentParts;
    }

    public bool IsNukeReady()
    {
        return isComplete;
    }

    private void OnDestroy()
    {
        // Clear the static reference when this object is destroyed
        if (activeNukeQuest == this)
        {
            activeNukeQuest = null;
        }
    }
}