using UnityEngine;

public class ContinuousLaserSystem : MonoBehaviour
{
    public GameObject laserPrefab;
    public Transform firePoint;
    public CylinderPlayerMovement playerMovement;
    public float fireRate = 0.1f;
    private float nextFireTime;

    void Update()
    {
        if (playerMovement.CanShoot && playerMovement.IsMoving && Time.time >= nextFireTime)
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
        laser.GetComponent<LaserProjectile>().Initialize(
            playerMovement.cylinderTransform,
            laserDir
        );
    }
}