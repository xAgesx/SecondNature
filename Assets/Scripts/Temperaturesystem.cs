using UnityEngine;
using UnityEngine.Events;

// ─────────────────────────────────────────────────────────────────────────────
// TemperatureSystem
// Attach to: XR Rig root
//
// Central stat manager. Call TemperatureSystem.Instance.AddHeat(amount, "Source")
// from anywhere to raise the player's body temperature.
// ─────────────────────────────────────────────────────────────────────────────
public class TemperatureSystem : MonoBehaviour
{
    public static TemperatureSystem Instance { get; private set; }

    [Header("Temperature Settings")]
    [Tooltip("Starting body temperature in Celsius.")]
    public float startingTemp = 34f;

    [Tooltip("Temperature at which game over triggers.")]
    public float maxTemp = 45f;

    [Header("VFX — Damage Flash")]
    [Tooltip("VFX GameObject on the XR Camera that plays on any heat damage.")]
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
        Debug.Log($"[Player] Spawned. Body temperature: {CurrentTemp:F1}°C. " +
                  $"Game over at {maxTemp:F1}°C — stay safe!");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Raises body temperature. Pass a readable source for debug logs.</summary>
    public void AddHeat(float amount, string source = "Unknown")
    {
        if (_isDead) return;

        CurrentTemp += amount;
        CurrentTemp = Mathf.Clamp(CurrentTemp, startingTemp, maxTemp);

        PlayDamageVFX();
        onTemperatureChanged?.Invoke(CurrentTemp);

        Debug.Log($"[TemperatureSystem] +{amount:F1}°C from '{source}'. " +
                  $"Current: {CurrentTemp:F1}°C / {maxTemp:F1}°C");

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
        Debug.LogError($"[Player] GAME OVER — body temperature reached {maxTemp:F1}°C!");
        onGameOver?.Invoke();
    }
}