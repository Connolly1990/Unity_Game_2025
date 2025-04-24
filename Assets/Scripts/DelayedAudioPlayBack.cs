using UnityEngine;

public class DelayedAudioPlayBack : MonoBehaviour
{
    public AudioSource audioSource; // Reference to the AudioSource
    public float delayInSeconds = 7f; // Delay duration

    private void Start()
    {
        Invoke("PlayAudio", delayInSeconds); // Schedule the audio to play after the delay
    }

    private void PlayAudio()
    {
        audioSource.Play(); // Play the audio
    }
}