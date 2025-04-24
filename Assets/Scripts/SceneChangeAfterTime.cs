using UnityEngine;
using UnityEngine.SceneManagement; // For scene management

public class SceneChangeAfterTime : MonoBehaviour
{
    public float timeToWait = 175f; // Time to wait before changing scene (in seconds, 175s = 2 minutes 55 seconds)

    private void Start()
    {
        // Start the countdown
        Invoke("ChangeScene", timeToWait);
    }

    private void ChangeScene()
    {
        // Log message for debugging
        Debug.Log("Time's up! Changing scene...");

        // Change to the next scene
        SceneManager.LoadScene("EndScene");
    }
}
