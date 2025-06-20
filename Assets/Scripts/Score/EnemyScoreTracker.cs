using UnityEngine;

public class EnemyScoreTracker : MonoBehaviour
{
    [Header("Score Settings")]
    [Tooltip("Points awarded when this enemy is destroyed")]
    public int pointsValue = 100;

    [Header("Drop Settings")]
    [Tooltip("Enemy part prefab to drop when destroyed")]
    public GameObject enemyPartPrefab;

    [Tooltip("Drop chance (1 = always drop, 5 = 1 in 5 chance, etc.)")]
    [Range(1, 20)]
    public int dropRate = 5;

    [Tooltip("Force spawn offset from enemy position (to avoid overlap)")]
    public Vector3 dropOffset = Vector3.up * 0.5f;

    private void OnDestroy()
    {
        // Don't award points when destroyed because of scene change or game exit
        if (!gameObject.scene.isLoaded)
            return;

        // Add score when enemy is destroyed
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointsValue);
        }

        // Check if we should drop an enemy part
        TryDropEnemyPart();

        // Notify the player that an enemy was killed
        CylinderPlayerMovement player = Object.FindFirstObjectByType<CylinderPlayerMovement>();
        if (player != null)
        {
            player.OnEnemyKilled();
        }
    }

    private void TryDropEnemyPart()
    {
        // Check if we have a prefab to drop
        if (enemyPartPrefab == null)
            return;

        // Roll for drop chance (1 in dropRate chance)
        int roll = Random.Range(1, dropRate + 1);

        if (roll == 1) // Success! Drop the part
        {
            SpawnEnemyPart();
        }
    }

    private void SpawnEnemyPart()
    {
        // Calculate spawn position with offset
        Vector3 spawnPosition = transform.position + dropOffset;

        // Spawn the enemy part with the same rotation as the enemy
        GameObject droppedPart = Instantiate(enemyPartPrefab, spawnPosition, transform.rotation);

        // Optional: Add some random rotation for visual variety
        if (droppedPart != null)
        {
            droppedPart.transform.Rotate(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        }
    }
}