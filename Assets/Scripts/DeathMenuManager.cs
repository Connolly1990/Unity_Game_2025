using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathMenuManager : MonoBehaviour
{
    public GameObject deathMenuCanvas;

    public void ShowDeathMenu()
    {
        deathMenuCanvas.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Quit()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
