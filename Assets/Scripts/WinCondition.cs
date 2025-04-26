using UnityEngine;
using TMPro;  // Import TextMeshPro namespace

public class WinCondition : MonoBehaviour
{
    public float winTime = 300f; // 300 seconds = 5 minutes
    private float timer = 0f;
    private bool hasWon = false;

    public GameObject winMenu;  // Assign your WinMenu here in the Inspector!
    public TMP_Text timerText;  // This is the TextMeshPro reference for the timer

    void Update()
    {
        if (hasWon)
            return;

        timer += Time.deltaTime;

        // Update the timer display
        UpdateTimerDisplay();

        if (timer >= winTime)
        {
            WinGame();
        }
    }

    private void UpdateTimerDisplay()
    {
        // Calculate remaining time
        float timeRemaining = winTime - timer;

        // Format the remaining time as minutes:seconds
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);

        // Update the timerText with formatted time
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void WinGame()
    {
        hasWon = true;
        Time.timeScale = 0f;  // Pause the game
        winMenu.SetActive(true);  // Show the Win Menu
        Cursor.lockState = CursorLockMode.None;  // Unlock cursor
        Cursor.visible = true;  // Make cursor visible
    }
}
