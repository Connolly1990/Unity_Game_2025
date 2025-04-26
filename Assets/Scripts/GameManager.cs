using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private int livesCount = 3;

    private int currentLives;
    private Vector3 lastDeathPosition;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentLives = livesCount;
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void PlayerDied(Vector3 deathPosition)
    {
        lastDeathPosition = deathPosition;
        currentLives--;

        if (currentLives <= 0)
        {
            Invoke("ShowGameOver", respawnDelay);
        }
        else
        {
            StartCoroutine(RespawnCoroutine());
        }
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnPlayer();
    }

    private void ShowGameOver()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    private void RespawnPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player)
        {
            player.transform.position = lastDeathPosition;
            player.SetActive(true);
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health) health.ResetHealth();
        }
    }

    public void RestartGame()
    {
        currentLives = livesCount;

        // Reset score when restarting game
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}