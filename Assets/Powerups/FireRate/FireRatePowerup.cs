using UnityEngine;
using TMPro;

public class FireRatePowerup : MonoBehaviour
{
    public static FireRatePowerup activeFireRateQuest;

    [Header("Fire Rate Quest Settings")]
    public int killsNeeded = 10;
    private int currentKills = 0;
    private bool isComplete = false;
    private bool questStarted = false;

    [Header("Fire Rate Boost Settings")]
    public float fireRateMultiplier = 2f; // x2 fire rate
    public float boostDuration = 30f; // 30 seconds
    private bool boostActive = false;
    private float boostEndTime;
    private float originalFireRate;

    [Header("References")]
    private ContinuousLaserSystem laserSystem;

    [Header("UI Settings")]
    private TextMeshProUGUI progressText;
    private GameObject progressUI;

    [Header("Audio Settings")]
    public AudioClip questStartSound;
    public AudioClip questCompleteSound;
    public AudioClip boostActivateSound;
    public AudioClip boostEndSound;
    public float soundVolume = 1f;

    private void Start()
    {
        // Find required components
        FindRequiredComponents();

        // Register this instance globally
        activeFireRateQuest = this;
        currentKills = 0;
        isComplete = false;
        questStarted = false;
        boostActive = false;

        // UI is already set to inactive in FindRequiredComponents()
        Debug.Log("Fire Rate Powerup ready to be picked up!");
    }

    private void FindRequiredComponents()
    {
        // Find the laser system
        laserSystem = Object.FindFirstObjectByType<ContinuousLaserSystem>();
        if (laserSystem == null)
        {
            Debug.LogError("FireRatePowerup: No ContinuousLaserSystem found in scene!");
        }

        // Find UI elements by tag (using Objective2 to differentiate from nuke)
        GameObject objectiveUI = GameObject.FindGameObjectWithTag("Objective2");
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
                Debug.LogError("FireRatePowerup: No TextMeshProUGUI component found on 'Objective2' GameObject or its children!");
            }

            // Ensure UI is initially hidden and ready for this new quest
            progressUI.SetActive(false);
        }
        else
        {
            Debug.LogError("FireRatePowerup: No GameObject with tag 'Objective2' found!");
        }
    }

    private void Update()
    {
        // Check if boost is active and should end
        if (boostActive && Time.time >= boostEndTime)
        {
            EndFireRateBoost();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartFireRateQuest();
            // Hide the powerup pickup but don't destroy it yet
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
        }
    }

    private void StartFireRateQuest()
    {
        questStarted = true;

        // Play quest start sound
        if (questStartSound != null)
        {
            AudioSource.PlayClipAtPoint(questStartSound, transform.position, soundVolume);
        }

        Debug.Log("Fire Rate challenge started! Get 10 kills to activate x2 fire rate for 30 seconds.");
        UpdateUI();
    }

    public void OnEnemyKilled()
    {
        // Only count kills if quest has been started by picking up the powerup
        if (!questStarted || isComplete) return;

        currentKills++;
        UpdateUI();

        if (currentKills >= killsNeeded)
        {
            CompleteFireRateQuest();
        }
    }

    public bool IsQuestStarted()
    {
        return questStarted;
    }

    private void CompleteFireRateQuest()
    {
        isComplete = true;

        if (questCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(questCompleteSound, transform.position, soundVolume);
        }

        Debug.Log("Fire Rate challenge complete! Activating x2 fire rate boost!");

        // Activate the fire rate boost immediately
        ActivateFireRateBoost();
    }

    private void ActivateFireRateBoost()
    {
        if (laserSystem == null) return;

        // Store original fire rate and apply boost
        originalFireRate = laserSystem.fireRate;
        laserSystem.fireRate = originalFireRate / fireRateMultiplier; // Divide to increase fire rate

        // Disable the overheat system during the boost
        laserSystem.DisableOverheat();

        boostActive = true;
        boostEndTime = Time.time + boostDuration;

        // Play boost activation sound
        if (boostActivateSound != null)
        {
            AudioSource.PlayClipAtPoint(boostActivateSound, transform.position, soundVolume);
        }

        Debug.Log($"Fire rate boosted by {fireRateMultiplier}x for {boostDuration} seconds! Overheat disabled.");
        UpdateUI();
    }

    private void EndFireRateBoost()
    {
        if (laserSystem == null) return;

        // Restore original fire rate
        laserSystem.fireRate = originalFireRate;

        // Re-enable the overheat system
        laserSystem.EnableOverheat();

        boostActive = false;

        // Play boost end sound
        if (boostEndSound != null)
        {
            AudioSource.PlayClipAtPoint(boostEndSound, transform.position, soundVolume);
        }

        Debug.Log("Fire rate boost ended! Overheat system re-enabled.");

        // Clean up after boost ends
        CleanupQuest();
    }

    private void CleanupQuest()
    {
        if (progressUI != null)
        {
            progressUI.SetActive(false);
        }

        // Clear the static reference and destroy this quest object
        // This ensures a new powerup must be picked up to start another quest
        activeFireRateQuest = null;
        Destroy(gameObject);
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            if (boostActive)
            {
                float timeRemaining = boostEndTime - Time.time;
                progressText.text = $"FIRE RATE BOOST: {timeRemaining:F1}s";
            }
            else if (!isComplete)
            {
                progressText.text = $"Kills for Fire Rate Boost: {currentKills}/{killsNeeded}";
            }
            else
            {
                progressText.text = "FIRE RATE BOOST READY!";
            }
        }

        // Show UI when quest starts
        if (progressUI != null && questStarted)
        {
            progressUI.SetActive(true);
        }
    }

    public void ResetFireRateQuest()
    {
        currentKills = 0;
        isComplete = false;
        questStarted = false;

        // End boost if it's currently active
        if (boostActive)
        {
            EndFireRateBoost();
        }

        UpdateUI();
    }

    public int GetKillsCollected()
    {
        return currentKills;
    }

    public bool IsBoostReady()
    {
        return isComplete;
    }

    public bool IsBoostActive()
    {
        return boostActive;
    }

    private void OnDestroy()
    {
        // Restore fire rate and overheat system if boost was active when destroyed
        if (boostActive && laserSystem != null)
        {
            laserSystem.fireRate = originalFireRate;
            laserSystem.EnableOverheat();
        }

        // Clear the static reference when this object is destroyed
        if (activeFireRateQuest == this)
        {
            activeFireRateQuest = null;
        }
    }
}