using UnityEngine;
using UnityEngine.Events;

// ─────────────────────────────────────────────────────────────────────────────
// TemperatureSystem
// Attach to: XR Rig (root player object)
//
// Central stat manager. Everything that damages the player calls
// TemperatureSystem.Instance.AddHeat(amount). Nothing else needs to know
// about UI, VFX, or game over — this class fires UnityEvents that other
// scripts listen to.
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

    // ── Events — hook these up in the Inspector or from other scripts ─────────
    // Fires every time temperature changes. Passes the new temperature value.
    public UnityEvent<float> onTemperatureChanged;
    // Fires once when maxTemp is reached.
    public UnityEvent onGameOver;

    // ── Runtime state ─────────────────────────────────────────────────────────
    public float CurrentTemp { get; private set; }
    private bool _isDead = false;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton so any damage source can reach this without a reference
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        CurrentTemp = startingTemp;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from any damage source to raise the player's temperature.
    /// e.g. TemperatureSystem.Instance.AddHeat(2f);
    /// </summary>
    public void AddHeat(float amount)
    {
        if (_isDead) return;

        CurrentTemp += amount;
        CurrentTemp = Mathf.Clamp(CurrentTemp, startingTemp, maxTemp);

        // Trigger damage VFX
        PlayDamageVFX();

        // Notify UI / anything else listening
        onTemperatureChanged?.Invoke(CurrentTemp);

        if (CurrentTemp >= maxTemp)
            TriggerGameOver();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void PlayDamageVFX()
    {
        if (damageVFX == null) return;

        // If it's a ParticleSystem, play it. Otherwise just activate it.
        var ps = damageVFX.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play();
        else
            damageVFX.SetActive(true);
    }

    private void TriggerGameOver()
    {
        _isDead = true;
        onGameOver?.Invoke();
        Debug.Log("[TemperatureSystem] Game Over — temp reached " + maxTemp);
    }
}