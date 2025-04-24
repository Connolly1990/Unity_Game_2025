using UnityEngine;

public class RandomSample : MonoBehaviour
{
    [SerializeField] private AudioClip[] breakableClips; // Audio clips for breakable objects
    [SerializeField] private AudioClip[] sliceableClips; // Audio clips for sliceable objects
    private AudioSource myAudioSource;

    private void Start()
    {
        // Get the AudioSource component attached to this GameObject
        myAudioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the other object has the correct tag and play corresponding audio
        if (other.CompareTag("Breakable"))
        {
            PlayRandomClip(breakableClips);
        }
        else if (other.CompareTag("Sliceable"))
        {
            PlayRandomClip(sliceableClips);
        }
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0)
        {
            AudioClip selectedClip = clips[Random.Range(0, clips.Length)];
            myAudioSource.PlayOneShot(selectedClip);
        }
    }
}
