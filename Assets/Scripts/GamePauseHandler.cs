using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine;

public class GamePauseHandler : MonoBehaviour
{
    [SerializeField] private List<GameObject> objectsToPause;

    private bool hasWeaponInLeftHand = false;
    private bool hasWeaponInRightHand = false;

    public void Start()
    {
        PauseGame();
    }

    public void PauseGame()
    {
        foreach (GameObject obj in objectsToPause)
        {
            if (obj.TryGetComponent(out PlayableDirector director))
            {
                director.Pause();
            }
            else
            {
                obj.SetActive(false);
            }
        }
    }

    public void UnpauseGame()
    {
        foreach (GameObject obj in objectsToPause)
        {
            if (obj.TryGetComponent(out PlayableDirector director))
            {
                director.Play();
            }
            else
            {
                obj.SetActive(true);
            }
        }
    }

    // Methods to handle when the player picks up weapons
    public void SetWeaponInLeftHand(bool isPickedUp)
    {
        hasWeaponInLeftHand = isPickedUp;
        CheckForWeaponPickup();
    }

    public void SetWeaponInRightHand(bool isPickedUp)
    {
        hasWeaponInRightHand = isPickedUp;
        CheckForWeaponPickup();
    }

    private void CheckForWeaponPickup()
    {
        if (hasWeaponInLeftHand && hasWeaponInRightHand)
        {
            UnpauseGame();
        }
    }
}
