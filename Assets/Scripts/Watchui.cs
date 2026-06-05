using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to your watch canvas root (or a child controller object).
/// Wire up the Slider and TMP_Text fields in the Inspector.
///
/// Slider setup in Inspector:
///   - Min Value : 0  (or match TemperatureSystem.startingTemp if you prefer)
///   - Max Value : 45 (or match TemperatureSystem.maxTemp)
///   - Direction : Bottom To Top
///   - Interactable : OFF
///
/// The slider fill will rise as temperature increases.
/// The text label shows current temp formatted as "XX.X°C".
/// </summary>
public class WatchUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The vertical slider on the watch face. Set Direction = Bottom To Top in the Inspector.")]
    public Slider tempSlider;

    [Tooltip("TextMeshPro text that displays the current temperature value.")]
    public TMP_Text tempLabel;

    [Header("Optional — Danger Indicator")]
    [Tooltip("Optional GameObject (e.g. a red glow image) shown when temp exceeds the danger threshold.")]
    public GameObject dangerIndicator;

    [Tooltip("Fraction of max temp at which the danger indicator activates. Default 0.75 = 75%.")]
    [Range(0f, 1f)]
    public float dangerThreshold = 0.75f;

    [Header("Colour Gradient (optional)")]
    [Tooltip("If assigned, the slider fill image colour will lerp along this gradient from min to max temp.")]
    public Gradient fillGradient;

    private Image _fillImage;
    private TemperatureSystem _tempSystem;

    private void Start()
    {
        _tempSystem = TemperatureSystem.Instance;

        if (_tempSystem == null)
        {
            Debug.LogError("[WatchUI] TemperatureSystem.Instance is null. " +
                           "Make sure TemperatureSystem exists in the scene and initialises before WatchUI.");
            enabled = false;
            return;
        }

        // Configure slider bounds to match the temperature system
        if (tempSlider != null)
        {
            tempSlider.minValue = _tempSystem.startingTemp;
            tempSlider.maxValue = _tempSystem.maxTemp;
            tempSlider.interactable = false;

            // Cache the fill image for gradient colour changes
            if (tempSlider.fillRect != null)
                _fillImage = tempSlider.fillRect.GetComponent<Image>();
        }
        else
        {
            Debug.LogWarning("[WatchUI] tempSlider is not assigned.");
        }

        // Subscribe to live temperature events
        _tempSystem.onTemperatureChanged.AddListener(OnTemperatureChanged);

        // Initialise UI to starting state
        RefreshUI(_tempSystem.CurrentTemp);

        // Hide danger indicator initially
        if (dangerIndicator != null)
            dangerIndicator.SetActive(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid stale delegate errors
        if (_tempSystem != null)
            _tempSystem.onTemperatureChanged.RemoveListener(OnTemperatureChanged);
    }

    /// <summary>Called by TemperatureSystem.onTemperatureChanged UnityEvent.</summary>
    private void OnTemperatureChanged(float newTemp)
    {
        RefreshUI(newTemp);
    }

    private void RefreshUI(float temp)
    {
        float min = _tempSystem.startingTemp;
        float max = _tempSystem.maxTemp;
        float t = Mathf.InverseLerp(min, max, temp); // 0 → 1

        // -- Slider --
        if (tempSlider != null)
            tempSlider.value = temp;

        // -- Fill gradient colour --
        if (_fillImage != null && fillGradient != null)
            _fillImage.color = fillGradient.Evaluate(t);

        // -- Label --
        if (tempLabel != null)
            tempLabel.text = $"{temp:F1}°C";

        // -- Danger indicator --
        if (dangerIndicator != null)
            dangerIndicator.SetActive(t >= dangerThreshold);
    }
}