using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DodgeUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dodgeText; // Use this if you're using TextMeshPro
                                      // public Text dodgeText; // Use this if you're using legacy UI Text

    [Header("Script References")]
    public DodgeController dodgeController;

    [Header("UI Settings")]
    public string readyText = "Double Tap a Direction to Dodge";
    public string cooldownText = "Double Tap a Direction to Dodge, Cooldown: ";
    public string dodgingText = "DODGING!";
    public int decimalPlaces = 1;

    [Header("Visual Feedback")]
    public Color readyColor = Color.white;
    public Color cooldownColor = Color.yellow;
    public Color dodgingColor = Color.cyan;

    // Private variables
    private float cooldownStartTime;
    private bool wasDodging = false;
    private bool wasOnCooldown = false;

    void Start()
    {
        // Try to find components if not assigned
        if (dodgeText == null)
        {
            dodgeText = GetComponent<TextMeshProUGUI>();
            if (dodgeText == null)
            {
                // Try legacy Text component if TextMeshPro not found
                Text legacyText = GetComponent<Text>();
                if (legacyText != null)
                {
                    Debug.LogWarning("DodgeUI: Using legacy Text component. Consider upgrading to TextMeshPro for better performance.");
                }
            }
        }

        if (dodgeController == null)
        {
            dodgeController = FindFirstObjectByType<DodgeController>();
            if (dodgeController == null)
            {
                Debug.LogError("DodgeUI: Could not find DodgeController in scene!");
                return;
            }
        }

        // Initialize UI
        UpdateUI();
    }

    void Update()
    {
        if (dodgeController == null || dodgeText == null) return;

        UpdateUI();
    }

    void UpdateUI()
    {
        bool isDodging = dodgeController.IsDodging;
        bool canDodge = dodgeController.CanDodge;
        bool isOnCooldown = !canDodge && !isDodging;

        // Track when dodge starts to record cooldown start time
        if (isDodging && !wasDodging)
        {
            cooldownStartTime = Time.time;
        }

        // Update text and color based on state
        if (isDodging)
        {
            // Currently dodging
            SetTextAndColor(dodgingText, dodgingColor);
        }
        else if (isOnCooldown)
        {
            // On cooldown - calculate remaining time
            float elapsedSinceDodge = Time.time - cooldownStartTime;
            float remainingCooldown = Mathf.Max(0f, dodgeController.dodgeCooldown - elapsedSinceDodge);

            if (remainingCooldown > 0f)
            {
                string cooldownDisplay = remainingCooldown.ToString("F" + decimalPlaces);
                string fullText = cooldownText + cooldownDisplay + "s";
                SetTextAndColor(fullText, cooldownColor);
            }
            else
            {
                // Cooldown should be over, show ready state
                SetTextAndColor(readyText, readyColor);
            }
        }
        else if (canDodge)
        {
            // Ready to dodge
            SetTextAndColor(readyText, readyColor);
        }

        // Update state tracking
        wasDodging = isDodging;
        wasOnCooldown = isOnCooldown;
    }

    void SetTextAndColor(string text, Color color)
    {
        if (dodgeText != null)
        {
            dodgeText.text = text;
            dodgeText.color = color;
        }
    }

    // Public method to manually refresh UI (useful for testing)
    public void RefreshUI()
    {
        UpdateUI();
    }

    // Method to set custom colors at runtime
    public void SetColors(Color ready, Color cooldown, Color dodging)
    {
        readyColor = ready;
        cooldownColor = cooldown;
        dodgingColor = dodging;
    }

    // Method to set custom text at runtime
    public void SetTexts(string ready, string cooldown, string dodging)
    {
        readyText = ready;
        cooldownText = cooldown;
        dodgingText = dodging;
    }
}