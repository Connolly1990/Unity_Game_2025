using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadAfterCredits()
    {
        SceneManager.LoadScene("AfterCredits"); 
    }
}
