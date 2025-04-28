using UnityEngine;
using UnityEngine.UI;

public class VolumeManagerSimple : MonoBehaviour
{
    private Slider volumeSlider;

    private void Start()
    {
        // Find inactive sliders too
        volumeSlider = FindObjectOfType<Slider>(true);

        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("Volume", 0.75f);
            volumeSlider.value = volume;
            SetVolume(volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        else
        {
            Debug.LogWarning("Volume Slider not found in scene!");
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
    }
}
