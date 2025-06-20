using UnityEngine;

public class PlayerClone : MonoBehaviour
{
    private GameObject originalPlayer;
    private float duration;
    private float timeRemaining;

    private Vector3 lastPlayerPosition;
    private Vector3 lastPlayerRotation;
    private bool wasPlayerShooting = false;

    private Transform playerTransform;
    private CylinderPlayerMovement playerMovement;
    private ContinuousLaserSystem playerLaserSystem;

    private float shootCooldown = 0f;

    public Transform firePoint; // Assign this on the clone for shooting direction
    public GameObject laserPrefab; // Optional override
    public AudioClip laserShootSound; // Optional override
    public AudioSource audioSource;

    public void Initialize(GameObject player, float cloneDuration)
    {
        originalPlayer = player;
        duration = cloneDuration;
        timeRemaining = duration;

        playerTransform = player.transform;
        playerMovement = player.GetComponent<CylinderPlayerMovement>();
        playerLaserSystem = player.GetComponent<ContinuousLaserSystem>();

        lastPlayerPosition = playerTransform.position;
        lastPlayerRotation = playerTransform.eulerAngles;
    }

    private void Update()
    {
        if (originalPlayer == null)
        {
            Destroy(gameObject);
            return;
        }

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            DestroyClone();
            return;
        }

        if (shootCooldown > 0f)
            shootCooldown -= Time.deltaTime;

        MimicPlayerMovement();
        MimicPlayerShooting();
    }

    private void MimicPlayerMovement()
    {
        Vector3 movementDelta = playerTransform.position - lastPlayerPosition;
        Vector3 rotationDelta = playerTransform.eulerAngles - lastPlayerRotation;

        transform.position += movementDelta;
        transform.rotation = Quaternion.Euler(transform.eulerAngles + rotationDelta);

        lastPlayerPosition = playerTransform.position;
        lastPlayerRotation = playerTransform.eulerAngles;
    }

    private void MimicPlayerShooting()
    {
        bool isPlayerShooting = false;

        if (playerLaserSystem != null && playerMovement != null)
        {
            isPlayerShooting = playerMovement.CanShoot && Input.GetKey(KeyCode.Space);
        }

        if (isPlayerShooting && !wasPlayerShooting && shootCooldown <= 0f)
        {
            CloneShoot();
            shootCooldown = 0.1f;
        }

        wasPlayerShooting = isPlayerShooting;
    }

    private void CloneShoot()
    {
        FireLaserCloneVersion();
    }

    private void FireLaserCloneVersion()
    {
        if (firePoint == null || playerMovement == null) return;

        Vector3 toCenter = firePoint.position - playerMovement.cylinderTransform.position;
        toCenter.y = 0;

        Vector3 tangent = Vector3.Cross(Vector3.up, toCenter.normalized).normalized;
        Vector3 laserDir = tangent * -playerMovement.CurrentDirection;

        GameObject laser = Instantiate(laserPrefab, firePoint.position, Quaternion.LookRotation(laserDir, Vector3.up));

        var laserComponent = laser.GetComponent<LaserProjectile>();
        if (laserComponent != null)
        {
            laserComponent.Initialize(playerMovement.cylinderTransform, laserDir);
        }

        if (laserShootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(laserShootSound);
        }
    }

    private void DestroyClone()
    {
        Debug.Log("Clone duration ended!");
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Debug.Log("Clone destroyed!");
    }
}
