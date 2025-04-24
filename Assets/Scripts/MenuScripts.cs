using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    // Function to start the game
    public void StartGame()
    {
        // Load the game scene (make sure the scene is added in Build Settings)
        SceneManager.LoadScene("Environment");
    }

    public void MainMenu()
    {
        // Load the game scene (make sure the scene is added in Build Settings)
        SceneManager.LoadScene("StartScene");
    }

    // Function to quit the game
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}