using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections; // Required for IEnumerator

public class WinCondition : MonoBehaviour
{
    public float winTime = 300f;
    private float timer = 0f;
    private bool hasWon = false;

    public GameObject winMenu;
    public TMP_Text timerText;
    public string afterCreditsSceneName = "AfterCredits"; // Set your scene name here

    void Update()
    {
        if (hasWon)
            return;

        timer += Time.deltaTime;
        UpdateTimerDisplay();

        if (timer >= winTime)
        {
            WinGame();
        }
    }

    private void UpdateTimerDisplay()
    {
        float timeRemaining = winTime - timer;
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void WinGame()
    {
        hasWon = true;
        Time.timeScale = 0f;
        winMenu.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(GoToAfterCredits());
    }

    private IEnumerator GoToAfterCredits()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(afterCreditsSceneName);
    }
}
