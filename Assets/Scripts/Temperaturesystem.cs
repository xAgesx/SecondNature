using UnityEngine;
using UnityEngine.Events;

// ─────────────────────────────────────────────────────────────────────────────
// TemperatureSystem
// Attach to: XR Rig (root player object)
//
// Central stat manager. Everything that damages the player calls
// TemperatureSystem.Instance.AddHeat(amount, "Source Name").
// ─────────────────────────────────────────────────────────────────────────────
public class TemperatureSystem : MonoBehaviour
{
    public static TemperatureSystem Instance { get; private set; }

    [Header("Temperature settings")]
    [Tooltip("Starting body temperature in celsius.")]
    public float startingTemp = 34f;

    [Tooltip("Temperature at which game over triggers.")]
    public float maxTemp = 45f;

    [Header("VFX — damage flash")]
    [Tooltip("The VFX GameObject on the XR Camera that plays when the player takes any heat damage.")]
    public GameObject damageVFX;

    // ── Events ────────────────────────────────────────────────────────────────
    public UnityEvent<float> onTemperatureChanged;
    public UnityEvent onGameOver;

    // ── Runtime state ─────────────────────────────────────────────────────────
    public float CurrentTemp { get; private set; }
    private bool _isDead = false;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        CurrentTemp = startingTemp;
    }

    private void Start()
    {
        Debug.Log($"[Player] You have spawned into the scene. " +
                  $"Current body temperature: {CurrentTemp:F1}°C. " +
                  $"Stay safe — game over at {maxTemp:F1}°C!");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Raises the player's temperature.
    /// Pass a human-readable source so debug logs are useful.
    /// </summary>
    public void AddHeat(float amount, string source = "Unknown")
    {
        if (_isDead) return;

        CurrentTemp += amount;
        CurrentTemp = Mathf.Clamp(CurrentTemp, startingTemp, maxTemp);

        PlayDamageVFX();
        onTemperatureChanged?.Invoke(CurrentTemp);

        if (CurrentTemp >= maxTemp)
            TriggerGameOver();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void PlayDamageVFX()
    {
        if (damageVFX == null) return;
        var ps = damageVFX.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
        else damageVFX.SetActive(true);
    }

    private void TriggerGameOver()
    {
        _isDead = true;
        Debug.LogError($"[Player] GAME OVER — your body temperature reached {maxTemp:F1}°C. " +
                       "You couldn't take the heat!");
        onGameOver?.Invoke();
    }
}