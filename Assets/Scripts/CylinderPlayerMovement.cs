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
    public float constantForwardSpeed = 1f; // Speed for constant forward movement

    [Header("Missile Settings")]
    public GameObject missilePrefab; // Drag your missile prefab here
    public float missileCooldown = 2f; // Time between missile shots
    public AudioClip missileFireSound; // Optional sound effect

    [Header("Voice Control")]
    public bool useVoiceControl = true;

    public bool CanShoot { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;
    public int CurrentDirection { get; private set; } = -1; // Start with forward direction (-1)

    private float currentAngle = 0f;
    private float cylinderRadius;
    private Rigidbody rb;
    private float horizontalInput = 0;
    private float verticalInput = 0;
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

        // We start with the ability to shoot since we're constantly moving
        CanShoot = true;
        hasMovedSinceStart = true;

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
        // Set up voice commands
        actions.Add("left", VoiceLeft);
        actions.Add("right", VoiceRight);
        actions.Add("up", VoiceUp);
        actions.Add("down", VoiceDown);
        actions.Add("stop", VoiceStop);
        actions.Add("fire", VoiceFire);

        // Initialize and start the keyword recognizer
        keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;
        keywordRecognizer.Start();

        Debug.Log("Voice control activated. Available commands: " + string.Join(", ", actions.Keys));
    }

    private void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        Debug.Log("Voice command recognized: " + speech.text);
        actions[speech.text].Invoke();
    }

    // Voice command methods
    private void VoiceLeft()
    {
        horizontalInput = 1; // Move left
        StartCoroutine(ResetHorizontalInputAfterDelay(0.5f));
    }

    private void VoiceRight()
    {
        horizontalInput = -1; // Move right
        StartCoroutine(ResetHorizontalInputAfterDelay(0.5f));
    }

    private void VoiceUp()
    {
        verticalInput = 1; // Move up
        StartCoroutine(ResetVerticalInputAfterDelay(0.5f));
    }

    private void VoiceDown()
    {
        verticalInput = -1; // Move down
        StartCoroutine(ResetVerticalInputAfterDelay(0.5f));
    }

    private void VoiceStop()
    {
        horizontalInput = 0;
        verticalInput = 0;
    }

    private void VoiceFire()
    {
        if (CanShoot && Time.time >= nextFireTime)
        {
            FireMissile();
        }
    }

    private IEnumerator ResetHorizontalInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        horizontalInput = 0;
    }

    private IEnumerator ResetVerticalInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        verticalInput = 0;
    }

    void Update()
    {
        // Only handle keyboard input if voice control is disabled
        if (!useVoiceControl)
        {
            HandleInput();
        }

        HandleMissileFiring();
    }

    void FixedUpdate()
    {
        // Apply constant forward movement - but separate from user input
        // This applies a consistent forward movement at the specified constant speed
        currentAngle -= constantForwardSpeed * Time.fixedDeltaTime / cylinderRadius;
        currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);

        // Now apply any additional user input (keyboard or voice)
        IsMoving = true; // Always moving because of constant forward motion

        if (horizontalInput != 0)
        {
            // Apply additional horizontal input from user
            currentAngle += horizontalInput * moveSpeed * Time.fixedDeltaTime / cylinderRadius;
            currentAngle = Mathf.Repeat(currentAngle, 2f * Mathf.PI);
        }

        // Calculate direction based on input
        int rawDirection = horizontalInput != 0 ? (int)Mathf.Sign(horizontalInput) : -1;

        // Calculate actual direction relative to cylinder tangent
        Vector3 toCenter = cylinderTransform.position - transform.position;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);

        // Update current direction
        CurrentDirection = rawDirection;

        // Vertical movement
        float newY = rb.position.y;
        if (verticalInput > 0 && canMoveUp)
            newY += moveSpeed * Time.fixedDeltaTime;
        else if (verticalInput < 0 && canMoveDown)
            newY -= moveSpeed * Time.fixedDeltaTime;

        UpdatePositionAndRotation(newY);
        UpdateShipOrientation();
    }

    void HandleInput()
    {
        // Get horizontal input (hold-based)
        horizontalInput = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontalInput += 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontalInput -= 1;

        // Get vertical input (hold-based)
        verticalInput = 0;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            verticalInput += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            verticalInput -= 1;
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
        if (CurrentDirection == 0)
            CurrentDirection = -1; // Default to forward

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