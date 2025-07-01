using UnityEngine;
using TMPro;

public class LaserNukePowerup : MonoBehaviour
{
    public static LaserNukePowerup activeNukeQuest;

    [Header("Nuke Quest Settings")]
    public int partsNeeded = 5;
    private int currentParts = 0;
    private bool isComplete = false;
    private bool questStarted = false;

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
        FindRequiredComponents();
        activeNukeQuest = this;
        currentParts = 0;
        isComplete = false;
        questStarted = false;
        Debug.Log("Laser Nuke Powerup ready to be picked up!");
    }

    private void FindRequiredComponents()
    {
        GameObject firePointObj = GameObject.FindGameObjectWithTag("FirePoint");
        if (firePointObj != null)
        {
            firePoint = firePointObj.transform;
        }
        else
        {
            Debug.LogError("LaserNukePowerup: No GameObject with tag 'FirePoint' found!");
        }

        playerMovement = Object.FindFirstObjectByType<CylinderPlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("LaserNukePowerup: No CylinderPlayerMovement found in scene!");
        }

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

            progressUI.SetActive(false);
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

    // This method should ONLY be called when picking up physical part objects
    public void OnPartCollected()
    {
        Debug.Log($"OnPartCollected() called! Quest Started: {questStarted}, Current Parts: {currentParts}, Is Complete: {isComplete}");

        if (!questStarted || isComplete)
        {
            Debug.Log("Part collection ignored - quest not started or already complete");
            return;
        }

        currentParts++;
        Debug.Log($"Part collected! Now have {currentParts}/{partsNeeded} parts");
        UpdateUI();

        if (currentParts >= partsNeeded)
        {
            CompleteNukeQuest();
        }
    }

    // This method can be called when enemies die (for other purposes like scoring)
    // but does NOT count toward nuke parts
    public void OnEnemyKilled()
    {
        Debug.Log("Enemy killed - but this doesn't count toward nuke parts!");
        // You can add other enemy death logic here if needed
        // Like updating score, playing sounds, etc.
        // But it won't affect the nuke quest
    }

    public bool IsQuestStarted()
    {
        return questStarted;
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
        FireNukeLaser();

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

        activeNukeQuest = null;
        Destroy(gameObject);
    }

    private void FireNukeLaser()
    {
        if (laserNukePrefab == null || firePoint == null || playerMovement == null) return;

        if (nukeLaunchSound != null)
        {
            AudioSource.PlayClipAtPoint(nukeLaunchSound, firePoint.position, soundVolume);
        }

        Vector3 toCenter = firePoint.position - playerMovement.cylinderTransform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;

        FireNukeInDirection(tangent);
        FireNukeInDirection(-tangent);

        Debug.Log("Laser Nukes fired in both directions!");
    }

    private void FireNukeInDirection(Vector3 direction)
    {
        GameObject nukeLaser = Instantiate(laserNukePrefab, firePoint.position,
                                        Quaternion.LookRotation(direction, Vector3.up));

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
        if (activeNukeQuest == this)
        {
            activeNukeQuest = null;
        }
    }
}