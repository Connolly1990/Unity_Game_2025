using UnityEngine;
using UnityEngine.UI;

public class VolumeManagerSimple : MonoBehaviour
{
    private static VolumeManagerSimple instance;
    private Slider volumeSlider;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (volumeSlider == null)
        {
            volumeSlider = FindObjectOfType<Slider>();

            if (volumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat("Volume", 0.75f);
                volumeSlider.value = volume;
                SetVolume(volume);
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
        }
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
    }
}
