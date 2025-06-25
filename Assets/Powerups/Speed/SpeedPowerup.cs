using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SpeedPowerup : MonoBehaviour
{
    public static SpeedPowerup activeSpeedQuest;

    [Header("Speed Quest Settings")]
    private GameObject ringContainer; // Found by tag "Rings"
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

    [Header("Visual Effects")]
    public GameObject pickupEffect; // Optional particle effect when picked up

    // Components to hide when picked up (merged from SpeedBoostPickup)
    private Renderer[] allRenderers;
    private Collider[] allColliders;
    private Light[] allLights;
    private ParticleSystem[] allParticles;

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

        // Update UI timer during boost
        if (boostActive && progressText != null)
        {
            float timeRemaining = boostEndTime - Time.time;
            progressText.text = $"SPEED BOOST: {timeRemaining:F1}s";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PickupSpeedPowerup();
        }
    }

    private void PickupSpeedPowerup()
    {
        // Play pickup sound
        if (questStartSound != null)
        {
            AudioSource.PlayClipAtPoint(questStartSound, transform.position, soundVolume);
        }

        // Spawn pickup effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, transform.rotation);
        }

        Debug.Log("Speed Powerup picked up! Starting ring collection quest...");

        // Hide ALL visuals using comprehensive method
        HideAllVisuals();

        StartSpeedQuest();
    }

    private void HideAllVisuals()
    {
        Debug.Log("=== HIDING ALL VISUALS (ENHANCED) ===");
        Debug.Log($"This GameObject name: {this.name}");

        // METHOD 1: Disable all child GameObjects (this should hide everything)
        Debug.Log("Method 1: Disabling all child GameObjects");
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Debug.Log($"Deactivating child: {child.name} (was active: {child.gameObject.activeSelf})");
            child.gameObject.SetActive(false);
        }

        // METHOD 2: Disable all renderers
        Debug.Log($"Method 2: Disabling {allRenderers.Length} renderers");
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        // METHOD 3: Disable all colliders except the trigger for cleanup
        Debug.Log($"Method 3: Disabling {allColliders.Length} colliders");
        foreach (Collider collider in allColliders)
        {
            if (collider != null && collider != GetComponent<Collider>())
            {
                collider.enabled = false;
            }
        }

        // METHOD 4: Disable all lights
        Debug.Log($"Method 4: Disabling {allLights.Length} lights");
        foreach (Light light in allLights)
        {
            if (light != null)
            {
                light.enabled = false;
            }
        }

        // METHOD 5: Stop all particle systems
        Debug.Log($"Method 5: Stopping {allParticles.Length} particle systems");
        foreach (ParticleSystem particle in allParticles)
        {
            if (particle != null)
            {
                particle.Stop();
                var emission = particle.emission;
                emission.enabled = false;
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

    // Debug method to verify what's still visible
    [ContextMenu("Debug Visible Components")]
    public void DebugVisibleComponents()
    {
        Debug.Log("=== DEBUGGING VISIBLE COMPONENTS ===");

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != this.transform)
            {
                Debug.Log($"Child: {child.name}, Active: {child.gameObject.activeSelf}, Parent: {child.parent.name}");

                ParticleSystem ps = child.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Debug.Log($"  - Has ParticleSystem: Playing={ps.isPlaying}, EmissionEnabled={ps.emission.enabled}");
                }

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