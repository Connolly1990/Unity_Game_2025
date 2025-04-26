using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    void Start()
    {
        // Reference the texts in ScoreManager if they're not assigned
        if (ScoreManager.Instance != null)
        {
            // Only set these references if they're not already set in the ScoreManager
            if (ScoreManager.Instance.scoreText == null && currentScoreText != null)
                ScoreManager.Instance.scoreText = currentScoreText;

            if (ScoreManager.Instance.highScoreText == null && highScoreText != null)
                ScoreManager.Instance.highScoreText = highScoreText;
        }
    }
}