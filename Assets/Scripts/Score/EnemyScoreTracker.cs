using UnityEngine;

public class EnemyScoreTracker : MonoBehaviour
{
    [Tooltip("Points awarded when this enemy is destroyed")]
    public int pointsValue = 100;

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
    }
}