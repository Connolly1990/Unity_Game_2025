using UnityEngine;

public class ContinuousLaserSystem : MonoBehaviour
{
    public GameObject laserPrefab;
    public Transform firePoint;
    public CylinderPlayerMovement playerMovement;
    public float fireRate = 0.1f;
    private float nextFireTime;

    // Audio variables
    public AudioSource audioSource; // Reference to the AudioSource component
    public AudioClip laserShootSound; // The "pew" sound effect for the laser

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
    }

    void Update()
    {
        // Check if player can shoot, spacebar is held, and fire rate cooldown is over
        if (playerMovement.CanShoot && Input.GetKey(KeyCode.Space) && Time.time >= nextFireTime)
        {
            FireLaser();
            nextFireTime = Time.time + fireRate;
        }
    }

    void FireLaser()
    {
        if (firePoint == null) return;

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
}