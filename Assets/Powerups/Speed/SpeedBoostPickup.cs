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

    // Components to hide when picked up
    private Renderer[] allRenderers;
    private Collider[] allColliders;
    private Light[] allLights;
    private ParticleSystem[] allParticles;

    // Static reference to prevent multiple boosts stacking
    private static SpeedBoostPickup currentActiveBoost;

    // Store original states for restoration
    private bool[] originalRendererStates;
    private bool[] originalColliderStates;
    private bool[] originalLightStates;
    private bool[] originalParticleStates;
    private GameObject[] originalChildStates;
    private bool[] childActiveStates;

    private void Start()
    {
        FindRequiredComponents();
        CacheComponents();
        StoreOriginalStates();
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

    private void CacheComponents()
    {
        // Cache all renderers, colliders, lights, and particle systems in this object and its children
        allRenderers = GetComponentsInChildren<Renderer>();
        allColliders = GetComponentsInChildren<Collider>();
        allLights = GetComponentsInChildren<Light>();
        allParticles = GetComponentsInChildren<ParticleSystem>();

        // Cache all child GameObjects
        originalChildStates = new GameObject[transform.childCount];
        childActiveStates = new bool[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            originalChildStates[i] = transform.GetChild(i).gameObject;
            childActiveStates[i] = originalChildStates[i].activeSelf;
        }

        Debug.Log($"Cached components - Renderers: {allRenderers.Length}, Colliders: {allColliders.Length}, Lights: {allLights.Length}, Particles: {allParticles.Length}, Children: {originalChildStates.Length}");
    }

    private void StoreOriginalStates()
    {
        // Store original enabled states
        originalRendererStates = new bool[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
        {
            originalRendererStates[i] = allRenderers[i].enabled;
        }

        originalColliderStates = new bool[allColliders.Length];
        for (int i = 0; i < allColliders.Length; i++)
        {
            originalColliderStates[i] = allColliders[i].enabled;
        }

        originalLightStates = new bool[allLights.Length];
        for (int i = 0; i < allLights.Length; i++)
        {
            originalLightStates[i] = allLights[i].enabled;
        }

        originalParticleStates = new bool[allParticles.Length];
        for (int i = 0; i < allParticles.Length; i++)
        {
            originalParticleStates[i] = allParticles[i].isPlaying;
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

        // Hide ALL visuals using multiple methods to ensure success
        HideAllVisuals();

        ActivateSpeedBoost();
    }

    private void HideAllVisuals()
    {
        Debug.Log("=== HIDING ALL VISUALS (ENHANCED) ===");
        Debug.Log($"This GameObject name: {this.name}");

      

        // METHOD 2: Disable all child GameObjects (this should hide everything)
        Debug.Log("Method 2: Disabling all child GameObjects");
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Debug.Log($"Deactivating child: {child.name} (was active: {child.gameObject.activeSelf})");
            child.gameObject.SetActive(false);
        }

        // METHOD 3: Disable all renderers
        Debug.Log($"Method 3: Disabling {allRenderers.Length} renderers");
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        // METHOD 4: Disable all colliders
        Debug.Log($"Method 4: Disabling {allColliders.Length} colliders");
        foreach (Collider collider in allColliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        // METHOD 5: Disable all lights
        Debug.Log($"Method 5: Disabling {allLights.Length} lights");
        foreach (Light light in allLights)
        {
            if (light != null)
            {
                light.enabled = false;
            }
        }

        // METHOD 6: Additional particle system search by tag and name
        Debug.Log("Method 6: Additional particle system cleanup");

        // Find by "Glow" tag
        GameObject[] glowObjects = GameObject.FindGameObjectsWithTag("Glow");
        foreach (GameObject glow in glowObjects)
        {
            if (glow.transform.IsChildOf(this.transform))
            {
                Debug.Log($"Found glow object: {glow.name} - deactivating");
                glow.SetActive(false);
            }
        }

        // Find any object with "glow" in the name (case insensitive)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name.ToLower().Contains("glow") || child.name.ToLower().Contains("particle"))
            {
                Debug.Log($"Found glow/particle object by name: {child.name} - deactivating");
                child.gameObject.SetActive(false);
            }
        }

        // METHOD 7: Final brute force - disable this entire GameObject's renderer if it has one
        Renderer thisRenderer = GetComponent<Renderer>();
        if (thisRenderer != null)
        {
            Debug.Log("Disabling main GameObject renderer");
            thisRenderer.enabled = false;
        }

        Debug.Log("=== ALL VISUALS HIDDEN (ENHANCED) ===");
    }

    private void ShowAllVisuals()
    {
        Debug.Log("=== RESTORING ALL VISUALS (ENHANCED) ===");

        // Restore child GameObjects
        for (int i = 0; i < originalChildStates.Length && i < childActiveStates.Length; i++)
        {
            if (originalChildStates[i] != null)
            {
                Debug.Log($"Restoring child: {originalChildStates[i].name} to active: {childActiveStates[i]}");
                originalChildStates[i].SetActive(childActiveStates[i]);
            }
        }

        // Restore renderers
        for (int i = 0; i < allRenderers.Length && i < originalRendererStates.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                allRenderers[i].enabled = originalRendererStates[i];
            }
        }

        // Restore colliders
        for (int i = 0; i < allColliders.Length && i < originalColliderStates.Length; i++)
        {
            if (allColliders[i] != null)
            {
                allColliders[i].enabled = originalColliderStates[i];
            }
        }

        // Restore lights
        for (int i = 0; i < allLights.Length && i < originalLightStates.Length; i++)
        {
            if (allLights[i] != null)
            {
                allLights[i].enabled = originalLightStates[i];
            }
        }

        // Restore particle systems
        for (int i = 0; i < allParticles.Length && i < originalParticleStates.Length; i++)
        {
            if (allParticles[i] != null)
            {
                

                // Re-enable emission
                var emission = allParticles[i].emission;
                emission.enabled = true;

                // Restart if it was originally playing
                if (originalParticleStates[i])
                {
                    allParticles[i].Play();
                }
            }
        }

        Debug.Log("=== ALL VISUALS RESTORED (ENHANCED) ===");
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

        Debug.Log($"Speed boosted by {speedMultiplier}x for {boostDuration} seconds! (Temporary boost)");
    }

    private void EndSpeedBoost()
    {
        if (playerMovement == null)
        {
            Debug.LogWarning("Player movement reference lost, cannot restore original speed!");
            boostActive = false;
            return;
        }

        // CRITICAL: Restore original speed (ensures boost is temporary)
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

        Debug.Log("Speed boost ended! Speed restored to original value.");

        // Destroy the pickup object if set to do so
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            // Re-enable for another pickup (including glow effects)
            ShowAllVisuals();
        }
    }

    // Public methods for external scripts to check boost status
    public bool IsBoostActive() => boostActive;
    public float GetTimeRemaining() => boostActive ? boostEndTime - Time.time : 0f;
    public float GetOriginalSpeed() => originalMoveSpeed;
    public float GetCurrentMultiplier() => boostActive ? speedMultiplier : 1f;

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
        // CRITICAL: Make sure to restore speed if this object is destroyed while boost is active
        if (boostActive && playerMovement != null)
        {
            playerMovement.moveSpeed = originalMoveSpeed;
            Debug.Log("SpeedBoostPickup destroyed while active - speed restored to prevent permanent boost!");
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

    // Debug method to verify boost is working correctly
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void OnGUI()
    {
        if (boostActive && playerMovement != null)
        {
            GUILayout.Label($"Boost Active: {boostActive}");
            GUILayout.Label($"Original Speed: {originalMoveSpeed}");
            GUILayout.Label($"Current Speed: {playerMovement.moveSpeed}");
            GUILayout.Label($"Time Remaining: {GetTimeRemaining():F1}s");
        }
    }

    // ADDITIONAL DEBUG METHOD - Call this to see what's still visible
    [ContextMenu("Debug Visible Components")]
    public void DebugVisibleComponents()
    {
        Debug.Log("=== DEBUGGING VISIBLE COMPONENTS ===");

        // Check all child objects
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != this.transform)
            {
                Debug.Log($"Child: {child.name}, Active: {child.gameObject.activeSelf}, Parent: {child.parent.name}");

                // Check for particle systems
                ParticleSystem ps = child.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Debug.Log($"  - Has ParticleSystem: Playing={ps.isPlaying}, EmissionEnabled={ps.emission.enabled}");
                }

                // Check for renderers
                Renderer r = child.GetComponent<Renderer>();
                if (r != null)
                {
                    Debug.Log($"  - Has Renderer: Enabled={r.enabled}");
                }
            }
        }

        Debug.Log("=== END DEBUG ===");
    }
}