using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class WeaponGrabHandler : MonoBehaviour
{
    public XRGrabInteractable grabInteractable;

    // Track which hands are holding weapons
    private static HashSet<string> leftHandWeapons = new HashSet<string>();
    private static HashSet<string> rightHandWeapons = new HashSet<string>();
    public static GamePauseHandler gamePauseHandler;

    // Flag to prevent multiple unpauses
    private static bool gameUnpaused = false;

    private void Awake()
    {
        // Get required components
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError($"WeaponGrabHandler on {gameObject.name}: XRGrabInteractable component is missing!");
            this.enabled = false;
            return;
        }

        // Find the GamePauseHandler if not already assigned
        if (gamePauseHandler == null)
        {
            gamePauseHandler = FindAnyObjectByType<GamePauseHandler>();
            if (gamePauseHandler == null)
            {
                Debug.LogError("WeaponGrabHandler: Could not find GamePauseHandler in the scene!");
            }
        }
    }

    public void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnSelectionExitAttempt);
        }
    }

    public void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnSelectionExitAttempt);
        }
    }

    public void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args == null || args.interactorObject == null)
        {
            Debug.LogWarning($"WeaponGrabHandler on {gameObject.name}: Received null args in OnGrabbed");
            return;
        }

        // Get unique identifier for this weapon
        string weaponId = gameObject.GetInstanceID().ToString();

        // Determine which hand grabbed the weapon and track it
        string handName = DetermineHandType(args.interactorObject);
        bool isLeftHand = handName == "LeftHand";
        bool isRightHand = handName == "RightHand";

        Debug.Log($"Weapon {weaponId} grabbed by {handName}. GameObject: {gameObject.name}");

        if (grabInteractable != null)
        {
            // Prevent dropping
            grabInteractable.throwOnDetach = false;
        }

        // Register this weapon with the appropriate hand
        if (isLeftHand)
        {
            leftHandWeapons.Add(weaponId);
            Debug.Log($"Added to left hand. Left hand weapons: {leftHandWeapons.Count}");
        }
        else if (isRightHand)
        {
            rightHandWeapons.Add(weaponId);
            Debug.Log($"Added to right hand. Right hand weapons: {rightHandWeapons.Count}");
        }

        // Check if both hands have weapons
        if (!gameUnpaused && leftHandWeapons.Count > 0 && rightHandWeapons.Count > 0)
        {
            Debug.Log("Both hands are now holding weapons!");

            if (gamePauseHandler != null)
            {
                Debug.Log("Calling UnpauseGame()...");
                try
                {
                    gamePauseHandler.UnpauseGame();
                    gameUnpaused = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error when trying to unpause game: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("GamePauseHandler is null - cannot unpause! Trying to find it again...");
                gamePauseHandler = FindAnyObjectByType<GamePauseHandler>();
                if (gamePauseHandler != null)
                {
                    Debug.Log("Found GamePauseHandler, attempting to unpause...");
                    gamePauseHandler.UnpauseGame();
                    gameUnpaused = true;
                }
            }
        }
    }

    public void OnSelectionExitAttempt(SelectExitEventArgs args)
    {
        if (args == null || args.interactorObject == null || grabInteractable == null ||
            grabInteractable.interactionManager == null)
        {
            return;
        }

        // Re-select the weapon to prevent dropping
        try
        {
            grabInteractable.interactionManager.SelectEnter(args.interactorObject, grabInteractable);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error when preventing weapon drop: {e.Message}");
        }
    }

    private string DetermineHandType(IXRSelectInteractor interactor)
    {
        // Default to unknown
        if (interactor == null)
        {
            return "Unknown";
        }

        // Get the GameObject of the interactor
        MonoBehaviour interactorMono = interactor as MonoBehaviour;
        if (interactorMono != null)
        {
            // First check object name
            string objName = interactorMono.name.ToLower();
            if (objName.Contains("left"))
            {
                return "LeftHand";
            }
            else if (objName.Contains("right"))
            {
                return "RightHand";
            }

            // Check the full path
            string fullPath = GetFullObjectPath(interactorMono.transform);
            if (fullPath.ToLower().Contains("left"))
            {
                return "LeftHand";
            }
            else if (fullPath.ToLower().Contains("right"))
            {
                return "RightHand";
            }

            // Check for modern XR Controller components using patterns
            foreach (var component in interactorMono.GetComponentsInParent<MonoBehaviour>())
            {
                string componentName = component.GetType().Name;
                if (componentName.Contains("Controller") || componentName.Contains("Hand"))
                {
                    if (component.name.ToLower().Contains("left"))
                    {
                        return "LeftHand";
                    }
                    else if (component.name.ToLower().Contains("right"))
                    {
                        return "RightHand";
                    }
                }
            }

            // Check for any interactor-related naming patterns in parents
            Transform current = interactorMono.transform;
            while (current != null)
            {
                if (current.name.ToLower().Contains("left"))
                {
                    return "LeftHand";
                }
                else if (current.name.ToLower().Contains("right"))
                {
                    return "RightHand";
                }
                current = current.parent;
            }
        }

        Debug.LogWarning($"Could not determine hand type for interactor: {interactor}");
        return "Unknown";
    }

    private string GetFullObjectPath(Transform transform)
    {
        if (transform == null)
        {
            return "null_transform";
        }

        string path = transform.name;
        Transform parent = transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    // Call this from your GameManager or scene initialization
    public static void ResetWeaponState()
    {
        leftHandWeapons.Clear();
        rightHandWeapons.Clear();
        gameUnpaused = false;

        if (gamePauseHandler == null)
        {
            gamePauseHandler = Object.FindAnyObjectByType<GamePauseHandler>();
        }

        Debug.Log("Weapon state has been reset");
    }
}