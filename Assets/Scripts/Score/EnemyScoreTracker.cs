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

    private bool hasBeenKilled = false; // Prevent double-processing

    // Public method to call when enemy is killed by projectile/damage
    public void OnEnemyDeath()
    {
        // Prevent multiple calls
        if (hasBeenKilled) return;
        hasBeenKilled = true;

        Debug.Log($"Enemy {gameObject.name} died! Awarding {pointsValue} points");

        // Add score when enemy is destroyed
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointsValue);
        }

        // Check if we should drop an enemy part
        TryDropEnemyPart();

        // Notify powerup quests about enemy death
        NotifyPowerupQuests();

        // Notify the player that an enemy was killed
        CylinderPlayerMovement player = Object.FindFirstObjectByType<CylinderPlayerMovement>();
        if (player != null)
        {
            player.OnEnemyKilled();
        }

        // Destroy the enemy after processing
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Fallback for when enemy is destroyed directly (like scene changes)
        // Only process if we haven't already been killed properly
        if (!hasBeenKilled && gameObject.scene.isLoaded)
        {
            Debug.Log($"Enemy {gameObject.name} destroyed via OnDestroy - processing death");
            OnEnemyDeath();
        }
    }

    private void NotifyPowerupQuests()
    {
        // Notify the fire rate quest about the kill (only if quest is started)
        if (FireRatePowerup.activeFireRateQuest != null && FireRatePowerup.activeFireRateQuest.IsQuestStarted())
        {
            FireRatePowerup.activeFireRateQuest.OnEnemyKilled();
        }

        // REMOVED: Don't notify nuke quest here - parts should only count when actually picked up!
        // The nuke quest will be notified when the player picks up the dropped part instead
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