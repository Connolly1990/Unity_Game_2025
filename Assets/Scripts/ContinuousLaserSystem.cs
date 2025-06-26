using UnityEngine;
using UnityEngine.UI;

public class ContinuousLaserSystem : MonoBehaviour
{
    [Header("Laser Settings")]
    public GameObject laserPrefab;
    public Transform firePoint;
    public CylinderPlayerMovement playerMovement;
    public float fireRate = 0.1f;
    private float nextFireTime;

    [Header("Overheat System")]
    public float maxHeat = 100f;
    public float heatPerShot = 8f;
    public float cooldownRate = 15f; // Heat units cooled per second
    public float overheatThreshold = 95f; // When to prevent firing
    public float overheatCooldownTime = 2f; // Extra cooldown time when overheated

    private float currentHeat = 0f;
    private bool isOverheated = false;
    private float overheatTimer = 0f;

    [Header("UI")]
    public Slider heatSlider; // Reference to the UI slider

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip laserShootSound;
    public AudioClip overheatSound; // Optional sound when overheating occurs

    void Start()
    {
        // Try to get the AudioSource component if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            // If there's still no AudioSource, add one
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Initialize the heat slider
        if (heatSlider != null)
        {
            heatSlider.maxValue = maxHeat;
            heatSlider.value = currentHeat;
        }
    }

    void Update()
    {
        UpdateHeatSystem();
        UpdateUI();

        // Check if player can shoot, spacebar is held, fire rate cooldown is over, and not overheated
        if (playerMovement.CanShoot &&
            Input.GetKey(KeyCode.Space) &&
            Time.time >= nextFireTime &&
            !isOverheated &&
            currentHeat < overheatThreshold)
        {
            FireLaser();
            nextFireTime = Time.time + fireRate;
        }
    }

    void UpdateHeatSystem()
    {
        // Handle overheat state
        if (isOverheated)
        {
            overheatTimer -= Time.deltaTime;
            if (overheatTimer <= 0f)
            {
                isOverheated = false;
            }
        }

        // Cool down the laser when not firing
        if (!Input.GetKey(KeyCode.Space) || isOverheated)
        {
            currentHeat -= cooldownRate * Time.deltaTime;
            currentHeat = Mathf.Max(0f, currentHeat);
        }

        // Check for overheat condition
        if (currentHeat >= maxHeat && !isOverheated)
        {
            TriggerOverheat();
        }
    }

    void UpdateUI()
    {
        if (heatSlider != null)
        {
            heatSlider.value = currentHeat;

            // Optional: Change slider color based on heat level
            Image fillImage = heatSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (isOverheated)
                {
                    fillImage.color = Color.red;
                }
                else if (currentHeat >= overheatThreshold)
                {
                    fillImage.color = Color.yellow;
                }
                else
                {
                    fillImage.color = Color.green;
                }
            }
        }
    }

    void TriggerOverheat()
    {
        isOverheated = true;
        overheatTimer = overheatCooldownTime;

        // Play overheat sound if available
        if (overheatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(overheatSound);
        }

        // Optional: Add visual feedback like screen shake or particle effect
        Debug.Log("Weapon Overheated!");
    }

    void FireLaser()
    {
        if (firePoint == null) return;

        // Add heat when firing
        currentHeat += heatPerShot;
        currentHeat = Mathf.Min(currentHeat, maxHeat);

        // Calculate tangent direction using cylinder math
        Vector3 toCenter = firePoint.position - playerMovement.cylinderTransform.position;
        toCenter.y = 0;
        // This will give us the correct tangent direction regardless of position on cylinder
        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;
        // Just flip the sign here (add negative sign)
        Vector3 laserDir = tangent * -playerMovement.CurrentDirection;

        GameObject laser = Instantiate(laserPrefab, firePoint.position,
                                    Quaternion.LookRotation(laserDir, Vector3.up));

        // Assuming your laser script has an Initialize method
        var laserComponent = laser.GetComponent<LaserProjectile>();
        if (laserComponent != null)
        {
            laserComponent.Initialize(
                playerMovement.cylinderTransform,
                laserDir
            );
        }

        // Play the laser shoot sound if available
        if (laserShootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(laserShootSound);
        }
    }

    // Public methods for external access (useful for UI or other systems)
    public float GetHeatPercentage()
    {
        return (currentHeat / maxHeat) * 100f;
    }

    public bool IsOverheated()
    {
        return isOverheated;
    }

    public float GetCurrentHeat()
    {
        return currentHeat;
    }
}