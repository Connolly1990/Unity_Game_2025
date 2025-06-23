using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SpeedPowerup : MonoBehaviour
{
    public static SpeedPowerup activeSpeedQuest;

    [Header("Speed Quest Settings")]
    private GameObject ringContainer; // Now found by tag
    private List<GameObject> rings = new List<GameObject>();
    private int totalRings = 0;
    private int ringsCollected = 0;
    private bool isComplete = false;
    private bool questStarted = false;

    [Header("Speed Boost Settings")]
    public float speedMultiplier = 2f;
    public float boostDuration = 30f;
    private bool boostActive = false;
    private float boostEndTime;
    private float originalMoveSpeed;

    [Header("References")]
    private CylinderPlayerMovement playerMovement;

    [Header("UI Settings")]
    private TextMeshProUGUI progressText;
    private GameObject progressUI;

    [Header("Audio Settings")]
    public AudioClip questStartSound;
    public AudioClip ringCollectSound;
    public AudioClip questCompleteSound;
    public AudioClip boostActivateSound;
    public AudioClip boostEndSound;
    public float soundVolume = 1f;

    private void Start()
    {
        FindRequiredComponents();
        SetupRings();

        activeSpeedQuest = this;
        ringsCollected = 0;
        isComplete = false;
        questStarted = false;
        boostActive = false;

        Debug.Log("Speed Powerup ready to be picked up!");
    }

    private void FindRequiredComponents()
    {
        playerMovement = Object.FindFirstObjectByType<CylinderPlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("SpeedPowerup: No CylinderPlayerMovement found in scene!");
        }

        // Find ring container by tag
        ringContainer = GameObject.FindGameObjectWithTag("Rings");
        if (ringContainer == null)
        {
            Debug.LogError("SpeedPowerup: No GameObject with tag 'Rings' found in the scene!");
        }

        GameObject objectiveUI = GameObject.FindGameObjectWithTag("Objective3");
        if (objectiveUI != null)
        {
            progressUI = objectiveUI;
            progressText = objectiveUI.GetComponent<TextMeshProUGUI>() ?? objectiveUI.GetComponentInChildren<TextMeshProUGUI>();

            if (progressText == null)
            {
                Debug.LogError("SpeedPowerup: No TextMeshProUGUI component found on 'Objective3' GameObject or its children!");
            }

            progressUI.SetActive(false);
        }
        else
        {
            Debug.LogError("SpeedPowerup: No GameObject with tag 'Objective3' found!");
        }
    }

    private void SetupRings()
    {
        if (ringContainer == null)
        {
            Debug.LogError("SpeedPowerup: Ring container not found!");
            return;
        }

        ringContainer.SetActive(false);
        rings.Clear();

        for (int i = 0; i < ringContainer.transform.childCount; i++)
        {
            GameObject ring = ringContainer.transform.GetChild(i).gameObject;
            rings.Add(ring);

            Collider ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null)
            {
                ringCollider.enabled = false;
            }

            SpeedRing speedRing = ring.GetComponent<SpeedRing>() ?? ring.AddComponent<SpeedRing>();
            speedRing.SetSpeedPowerup(this);
        }

        totalRings = rings.Count;
        Debug.Log($"Setup {totalRings} rings for speed quest");
    }

    private void Update()
    {
        if (boostActive && Time.time >= boostEndTime)
        {
            EndSpeedBoost();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartSpeedQuest();
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
        }
    }

    private void StartSpeedQuest()
    {
        questStarted = true;

        if (ringContainer != null)
        {
            ringContainer.SetActive(true);
        }

        foreach (GameObject ring in rings)
        {
            Collider ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null)
            {
                ringCollider.enabled = true;
            }
        }

        if (questStartSound != null)
        {
            AudioSource.PlayClipAtPoint(questStartSound, transform.position, soundVolume);
        }

        Debug.Log($"Speed challenge started! Fly through all {totalRings} rings to activate x2 speed for 30 seconds.");
        UpdateUI();
    }

    public void OnRingCollected(GameObject ring)
    {
        if (!questStarted || isComplete) return;

        ringsCollected++;

        if (ringCollectSound != null)
        {
            AudioSource.PlayClipAtPoint(ringCollectSound, ring.transform.position, soundVolume);
        }

        ring.SetActive(false);

        Debug.Log($"Ring collected! {ringsCollected}/{totalRings}");
        UpdateUI();

        if (ringsCollected >= totalRings)
        {
            CompleteSpeedQuest();
        }
    }

    public bool IsQuestStarted() => questStarted;

    private void CompleteSpeedQuest()
    {
        isComplete = true;

        if (questCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(questCompleteSound, transform.position, soundVolume);
        }

        Debug.Log("Speed challenge complete! Activating x2 speed boost!");
        ActivateSpeedBoost();
    }

    private void ActivateSpeedBoost()
    {
        if (playerMovement == null) return;

        originalMoveSpeed = playerMovement.moveSpeed;
        playerMovement.moveSpeed = originalMoveSpeed * speedMultiplier;

        boostActive = true;
        boostEndTime = Time.time + boostDuration;

        if (boostActivateSound != null)
        {
            AudioSource.PlayClipAtPoint(boostActivateSound, transform.position, soundVolume);
        }

        Debug.Log($"Speed boosted by {speedMultiplier}x for {boostDuration} seconds!");
        UpdateUI();
    }

    private void EndSpeedBoost()
    {
        if (playerMovement == null) return;

        playerMovement.moveSpeed = originalMoveSpeed;
        boostActive = false;

        if (boostEndSound != null)
        {
            AudioSource.PlayClipAtPoint(boostEndSound, transform.position, soundVolume);
        }

        Debug.Log("Speed boost ended!");
        CleanupQuest();
    }

    private void CleanupQuest()
    {
        if (progressUI != null)
        {
            progressUI.SetActive(false);
        }

        ResetRings();

        activeSpeedQuest = null;
        Destroy(gameObject);
    }

    private void ResetRings()
    {
        foreach (GameObject ring in rings)
        {
            if (ring != null)
            {
                ring.SetActive(true);
                Collider ringCollider = ring.GetComponent<Collider>();
                if (ringCollider != null)
                {
                    ringCollider.enabled = false;
                }
            }
        }

        if (ringContainer != null)
        {
            ringContainer.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            if (boostActive)
            {
                float timeRemaining = boostEndTime - Time.time;
                progressText.text = $"SPEED BOOST: {timeRemaining:F1}s";
            }
            else if (!isComplete)
            {
                progressText.text = $"Rings Collected: {ringsCollected}/{totalRings}";
            }
            else
            {
                progressText.text = "SPEED BOOST READY!";
            }
        }

        if (progressUI != null && questStarted)
        {
            progressUI.SetActive(true);
        }
    }

    public void ResetSpeedQuest()
    {
        ringsCollected = 0;
        isComplete = false;
        questStarted = false;
        boostActive = false;

        if (playerMovement != null && boostActive)
        {
            playerMovement.moveSpeed = originalMoveSpeed;
        }

        ResetRings();
        UpdateUI();
    }

    public int GetRingsCollected() => ringsCollected;
    public bool IsBoostReady() => isComplete;
    public bool IsBoostActive() => boostActive;

    private void OnDestroy()
    {
        if (boostActive && playerMovement != null)
        {
            playerMovement.moveSpeed = originalMoveSpeed;
        }

        ResetRings();

        if (activeSpeedQuest == this)
        {
            activeSpeedQuest = null;
        }
    }
}

public class SpeedRing : MonoBehaviour
{
    private SpeedPowerup speedPowerup;

    public void SetSpeedPowerup(SpeedPowerup powerup)
    {
        speedPowerup = powerup;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && speedPowerup != null && speedPowerup.IsQuestStarted())
        {
            speedPowerup.OnRingCollected(gameObject);
        }
    }
}
