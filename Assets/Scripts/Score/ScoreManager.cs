using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private int defaultPointsPerKill = 100;

    [Header("UI References")]
    // Make these public so they can be accessed by ScoreDisplay
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    private int currentScore = 0;
    private int highScore = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load high score from PlayerPrefs
        LoadHighScore();
    }

    void Start()
    {
        // Initialize UI
        UpdateScoreUI();
        UpdateHighScoreUI();
    }

    public void AddScore(int points = 0)
    {
        // If no points specified, use default points
        if (points <= 0)
            points = defaultPointsPerKill;

        // Add to current score
        currentScore += points;

        // Check for high score
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
        }

        // Update UI
        UpdateScoreUI();
        UpdateHighScoreUI();
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("N0");
        }
    }

    private void UpdateHighScoreUI()
    {
        if (highScoreText != null)
        {
            highScoreText.text = "HighScore: " + highScore.ToString("N0");
        }
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public int GetHighScore()
    {
        return highScore;
    }
}