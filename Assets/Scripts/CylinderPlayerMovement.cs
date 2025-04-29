using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

[RequireComponent(typeof(Rigidbody))]
public class CylinderPlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cylinderTransform;
    public Transform playerModel;
    public Transform missileSpawnPoint; // Reference to the missile spawn point

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;
    public float boundaryOffset = 0.5f;

    [Header("Missile Settings")]
    public GameObject missilePrefab; // Drag your missile prefab here
    public float missileCooldown = 2f; // Time between missile shots
    public AudioClip missileFireSound; // Optional sound effect

    [Header("Voice Control")]
    public bool useVoiceControl = true;

    public bool CanShoot { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;
    public int CurrentDirection { get; private set; } = 0; // 0=idle, 1=forward, -1=backward

    private float currentAngle = 0f;
    private float cylinderRadius;
    private Rigidbody rb;
    private int horizontalDirection = 0;
    private int verticalDirection = 0;
    private bool canMoveUp = true;
    private bool canMoveDown = true;
    private bool hasMovedSinceStart = false;

    // Missile firing variables
    private float nextFireTime = 0f;
    private AudioSource audioSource;

    // Voice Control variables
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, Action> actions = new Dictionary<string, Action>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        cylinderRadius = cylinderTransform.localScale.x * 0.5f;
        currentAngle = Mathf.Atan2(transform.position.x - cylinderTransform.position.x,
                                 transform.position.z - cylinderTransform.position.z);
        UpdatePositionAndRotation(transform.position.y, true);
        CanShoot = false;

        // Get or add AudioSource component for missile sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && missileFireSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Validate missile spawn point
        if (missileSpawnPoint == null)
        {
            Debug.LogWarning("Missile spawn point not assigned. Using player position instead.");
            missileSpawnPoint = transform;
        }

        // Set up voice control if enabled
        if (useVoiceControl)
        {
            SetupVoiceControl();
        }
    }

    private void SetupVoiceControl()
    {
        // Set up voice commands with variations to improve recognition
        actions.Add("left", VoiceLeft);
        actions.Add("right", VoiceRight);
        actions.Add("up", VoiceUp);
        actions.Add("upward", VoiceUp);  // Adding variation for "up"
        actions.Add("move up", VoiceUp); // Adding another variation
        actions.Add("down", VoiceDown);
        actions.Add("fire", VoiceFire);
        actions.Add("stop", VoiceStop);  // Added stop command for convenience

        // Initialize and start the keyword recognizer
        keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray(), ConfidenceLevel.Low);
        keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;
        keywordRecognizer.Start();

        Debug.Log("Voice control activated. Available commands: " + string.Join(", ", actions.Keys));

        // Extra debug for "up" command specifically
        Debug.Log("UP command specifically registered with recognizer");
    }

    private void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        string recognizedText = speech.text.ToLower().Trim();
        Debug.Log("Voice command recognized: '" + recognizedText + "'");

        // Extra debug for any "up"-related commands
        if (recognizedText.Contains("up"))
        {
            Debug.Log("UP DETECTED in speech: '" + recognizedText + "'");
        }

        if (actions.ContainsKey(recognizedText))
        {
            Debug.Log("Executing command: " + recognizedText);
            actions[recognizedText].Invoke();
        }
        else
        {
            Debug.LogWarning("Command not found in actions dictionary: '" + recognizedText + "'");
            Debug.LogWarning("Available commands: " + string.Join(", ", actions.Keys));
        }
    }

    // Voice command methods
    private void VoiceLeft()
    {
        Debug.Log("Voice Left Command Executed");
        horizontalDirection = 1;  // Changed to directly set direction
    }

    private void VoiceRight()
    {
        Debug.Log("Voice Right Command Executed");
        horizontalDirection = -1;  // Changed to directly set direction
    }

    private void VoiceUp()
    {
        Debug.Log("**** VOICE UP COMMAND EXECUTED ****");
        verticalDirection = 1;  // Changed to directly set direction

        // Force movement for a short time to ensure it's recognized
        StartCoroutine(EnsureMovementCoroutine("up"));
    }

    private IEnumerator EnsureMovementCoroutine(string direction)
    {
        Debug.Log("Ensuring " + direction + " movement is applied");

        // For "up" direction, make sure any potential obstacles are cleared
        if (direction == "up")
        {
            canMoveUp = true;
        }

        // Wait to ensure the movement is applied in the next FixedUpdate
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Debug.Log("Movement should now be applied for: " + direction);
    }

    private void VoiceDown()
    {
        Debug.Log("Voice Down Command Executed");
        verticalDirection = -1;  // Changed to directly set direction
    }

    private void VoiceStop()
    {
        Debug.Log("Voice Stop Command Executed");
        horizontalDirection = 0;
        verticalDirection = 0;
    }

    private void VoiceFire()
    {
        Debug.Log("Voice Fire Command Executed");
        if (CanShoot && Time.time >= nextFireTime)
        {
            FireMissile();
        }
    }

    void Update()
    {
        HandleInput();
        HandleMissileFiring();
    }

    void FixedUpdate()
    {
        IsMoving = horizontalDirection != 0 || verticalDirection != 0;
        if (IsMoving && !hasMovedSinceStart)
        {
            hasMovedSinceStart = true;
            CanShoot = true;
        }

        // Update raw direction based on input
        int rawDirection = (int)Mathf.Sign(horizontalDirection);

        // Update position using angle (normalized to prevent overflow)
        currentAngle += horizontalDirection * moveSpeed * Time.fixedDeltaTime / cylinderRadius;
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Calculate actual direction relative to cylinder tangent
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        // This will ensure CurrentDirection is relative to the player's orientation on the cylinder
        if (rawDirection != 0)
            CurrentDirection = rawDirection;

        // Vertical movement with boundary check
        float newY = rb.position.y;

        // Check if can move up (add your boundary logic here if needed)
        if (verticalDirection > 0)
        {
            // Add any upper boundary check here
            canMoveUp = true; // Or implement your boundary check
            if (canMoveUp)
                newY += moveSpeed * Time.fixedDeltaTime;
        }
        // Check if can move down (add your boundary logic here if needed)
        else if (verticalDirection < 0)
        {
            // Add any lower boundary check here
            canMoveDown = true; // Or implement your boundary check
            if (canMoveDown)
                newY -= moveSpeed * Time.fixedDeltaTime;
        }

        UpdatePositionAndRotation(newY);
        UpdateShipOrientation();
    }

    void HandleInput()
    {
        // Horizontal input (toggle-based)
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            horizontalDirection = horizontalDirection == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            horizontalDirection = horizontalDirection == -1 ? 0 : -1;

        // Vertical input (toggle-based)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            verticalDirection = verticalDirection == 1 ? 0 : 1;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            verticalDirection = verticalDirection == -1 ? 0 : -1;
    }

    void HandleMissileFiring()
    {
        // Check if player can shoot and F key is pressed
        if (CanShoot && Input.GetKeyDown(KeyCode.F) && Time.time >= nextFireTime)
        {
            FireMissile();
        }
    }

    void FireMissile()
    {
        if (missilePrefab == null)
        {
            Debug.LogError("Missile prefab not assigned!");
            return;
        }

        // Set the cooldown for next shot
        nextFireTime = Time.time + missileCooldown;

        // Get the forward direction considering player orientation on cylinder
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        Vector3 launchDirection = tangent * CurrentDirection;

        // Instantiate missile at spawn point
        GameObject missile = Instantiate(missilePrefab, missileSpawnPoint.position, Quaternion.identity);

        // Initialize the missile
        HomingMissile homingMissile = missile.GetComponent<HomingMissile>();
        if (homingMissile != null)
        {
            homingMissile.Initialize(launchDirection);
        }

        // Play sound effect if available
        if (audioSource != null && missileFireSound != null)
        {
            audioSource.PlayOneShot(missileFireSound);
        }
    }

    void UpdatePositionAndRotation(float newY, bool snap = false)
    {
        Vector3 targetPosition = new Vector3(
            cylinderRadius * Mathf.Sin(currentAngle),
            newY,
            cylinderRadius * Mathf.Cos(currentAngle)
        );

        Vector3 toCenter = cylinderTransform.position - targetPosition;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(tangent * CurrentDirection, Vector3.up)
                                 * Quaternion.Euler(270, 0, 0);

        if (snap)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            rb.MovePosition(targetPosition);
            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation,
                           rotationSpeed * Time.fixedDeltaTime));
        }
    }

    void UpdateShipOrientation()
    {
        if (CurrentDirection == 0) return;

        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        playerModel.rotation = Quaternion.LookRotation(tangent * CurrentDirection, Vector3.up)
                             * Quaternion.Euler(270, 0, 0);
    }

    void OnDrawGizmos()
    {
        if (cylinderTransform == null) return;

        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, tangent * CurrentDirection * 2f);

        // Draw missile spawn point
        if (missileSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(missileSpawnPoint.position, 0.2f);
        }
    }

    private void OnDestroy()
    {
        // Clean up voice recognition when object is destroyed
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }
    }
}