using UnityEngine;
using TMPro;

public class SpeedBoostPickup : MonoBehaviour
{
    [Header("Speed Boost Settings")]
    public float speedMultiplier = 2f;
    public float boostDuration = 30f;
    private bool boostActive = false;
    private float boostEndTime;
    private float originalMoveSpeed;

    [Header("References")]
    private CylinderPlayerMovement playerMovement;

    [Header("UI Settings")]
    private TextMeshProUGUI boostUI;
    private GameObject boostUIObject;

    [Header("Audio Settings")]
    public AudioClip pickupSound;
    public AudioClip boostActivateSound;
    public AudioClip boostEndSound;
    public float soundVolume = 1f;

    [Header("Visual Effects")]
    public GameObject pickupEffect; // Optional particle effect when picked up
    public bool destroyOnPickup = true;

    // Static reference to prevent multiple boosts stacking
    private static SpeedBoostPickup currentActiveBoost;

    private void Start()
    {
        FindRequiredComponents();
        Debug.Log("Speed Boost Pickup ready!");
    }

    private void FindRequiredComponents()
    {
        // Find the player movement component
        playerMovement = Object.FindFirstObjectByType<CylinderPlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("SpeedBoostPickup: No CylinderPlayerMovement found in scene!");
        }

        // Find UI element for boost timer (you can change the tag name to match your UI)
        GameObject uiObject = GameObject.FindGameObjectWithTag("SpeedBoostUI");
        if (uiObject != null)
        {
            boostUIObject = uiObject;
            boostUI = uiObject.GetComponent<TextMeshProUGUI>() ?? uiObject.GetComponentInChildren<TextMeshProUGUI>();

            if (boostUI != null)
            {
                boostUIObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("SpeedBoostPickup: No GameObject with tag 'SpeedBoostUI' found. Timer won't be displayed.");
        }
    }

    private void Update()
    {
        // Check if boost duration has ended
        if (boostActive && Time.time >= boostEndTime)
        {
            EndSpeedBoost();
        }

        // Update UI timer
        if (boostActive && boostUI != null)
        {
            float timeRemaining = boostEndTime - Time.time;
            boostUI.text = $"SPEED BOOST: {timeRemaining:F1}s";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PickupSpeedBoost();
        }
    }

    private void PickupSpeedBoost()
    {
        // Check if another speed boost is already active
        if (currentActiveBoost != null && currentActiveBoost != this)
        {
            // End the previous boost before starting new one
            currentActiveBoost.EndSpeedBoost();
        }

        // Play pickup sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
        }

        // Spawn pickup effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, transform.rotation);
        }

        Debug.Log("Speed Boost picked up! Activating speed boost...");

        // Hide the pickup visually but don't destroy yet (we need to manage the boost)
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;

        ActivateSpeedBoost();
    }

    private void ActivateSpeedBoost()
    {
        if (playerMovement == null)
        {
            Debug.LogError("Cannot activate speed boost - no player movement found!");
            return;
        }

        // Set this as the current active boost
        currentActiveBoost = this;

        // Store original speed and apply boost
        originalMoveSpeed = playerMovement.moveSpeed;
        playerMovement.moveSpeed = originalMoveSpeed * speedMultiplier;

        // Set boost duration
        boostActive = true;
        boostEndTime = Time.time + boostDuration;

        // Play boost activate sound
        if (boostActivateSound != null)
        {
            AudioSource.PlayClipAtPoint(boostActivateSound, transform.position, soundVolume);
        }

        // Show UI
        if (boostUIObject != null)
        {
            boostUIObject.SetActive(true);
        }

        Debug.Log($"Speed boosted by {speedMultiplier}x for {boostDuration} seconds!");
    }

    private void EndSpeedBoost()
    {
        if (playerMovement == null) return;

        // Restore original speed
        playerMovement.moveSpeed = originalMoveSpeed;
        boostActive = false;

        // Play boost end sound
        if (boostEndSound != null)
        {
            AudioSource.PlayClipAtPoint(boostEndSound, transform.position, soundVolume);
        }

        // Hide UI
        if (boostUIObject != null)
        {
            boostUIObject.SetActive(false);
        }

        // Clear static reference
        if (currentActiveBoost == this)
        {
            currentActiveBoost = null;
        }

        Debug.Log("Speed boost ended!");

        // Destroy the pickup object if set to do so
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            // Re-enable for another pickup
            GetComponent<Renderer>().enabled = true;
            GetComponent<Collider>().enabled = true;
        }
    }

    // Public methods for external scripts to check boost status
    public bool IsBoostActive() => boostActive;
    public float GetTimeRemaining() => boostActive ? boostEndTime - Time.time : 0f;

    // Method to manually end boost (useful for game events)
    public void ForceEndBoost()
    {
        if (boostActive)
        {
            EndSpeedBoost();
        }
    }

    private void OnDestroy()
    {
        // Make sure to restore speed if this object is destroyed while boost is active
        if (boostActive && playerMovement != null)
        {
            playerMovement.moveSpeed = originalMoveSpeed;
        }

        // Clear static reference
        if (currentActiveBoost == this)
        {
            currentActiveBoost = null;
        }

        // Hide UI
        if (boostUIObject != null)
        {
            boostUIObject.SetActive(false);
        }
    }
}