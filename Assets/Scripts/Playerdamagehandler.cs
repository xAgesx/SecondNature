using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// PlayerDamageHandler
// Attach to: XR Rig root (same object as TemperatureSystem)
//
// All damage types live here. Toggle each one on/off in the Inspector.
// Hand contact is driven by DangerousInteractable on Door/Window objects.
// ─────────────────────────────────────────────────────────────────────────────
public class PlayerDamageHandler : MonoBehaviour
{
    // ── Bare hand contact ─────────────────────────────────────────────────────

    [Header("Bare hand contact")]
    [Tooltip("Enable damage when a bare hand grabs a Door or Window interactable.")]
    public bool bareHandDamageEnabled = true;

    [Tooltip("Heat added on bare hand contact.")]
    public float bareHandHeat = 2f;

    [Tooltip("VFX GameObject on the XR Camera to play on bare hand damage.")]
    public GameObject bareHandVFX;

    // ── Smoke zone ────────────────────────────────────────────────────────────

    [Header("Smoke zone")]
    [Tooltip("Enable damage over time while inside a smoke zone.")]
    public bool smokeDamageEnabled = true;

    [Tooltip("Heat added per tick inside smoke.")]
    public float smokeHeatPerTick = 1f;

    [Tooltip("Seconds between each smoke damage tick.")]
    public float smokeTickInterval = 3f;

    [Tooltip("VFX GameObject on the XR Camera to show while inside smoke.")]
    public GameObject smokeOverlayVFX;

    // ── Fire zone ─────────────────────────────────────────────────────────────

    [Header("Fire zone")]
    [Tooltip("Enable damage when entering a fire zone.")]
    public bool fireDamageEnabled = true;

    [Tooltip("Heat added on entering a fire zone.")]
    public float fireHeatOnEnter = 5f;

    [Tooltip("Heat added per tick inside fire (continuous damage).")]
    public float fireHeatPerTick = 2f;

    [Tooltip("Seconds between each fire damage tick.")]
    public float fireTickInterval = 2f;

    [Tooltip("VFX GameObject on the XR Camera to play on fire entry.")]
    public GameObject fireOverlayVFX;

    [Tooltip("How long the fire VFX stays on after entering (seconds). 0 = until exit.")]
    public float fireVFXDuration = 1.5f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private bool _inSmokeZone = false;
    private bool _inFireZone = false;
    private Coroutine _smokeDamageCoroutine;
    private Coroutine _fireDamageCoroutine;

    // ── Bare hand contact (called by DangerousInteractable) ───────────────────

    /// <param name="objectName">Name of the grabbed object, used in the console message.</param>
    public void OnBareHandTouchedSurface(string objectName = "object")
    {
        if (!bareHandDamageEnabled) return;

        float tempBefore = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        ScoreTracker.Instance?.RegisterError();
        TemperatureSystem.Instance.AddHeat(bareHandHeat, "Bare Hand Contact");
        float tempAfter = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;

        Debug.LogWarning($"[Player] Oh no! You touched the {objectName} with your bare hands! " +
                         $"Your temperature jumped from {tempBefore:F1}°C to {tempAfter:F1}°C.");

        PlayVFX(bareHandVFX);
    }

    // ── Zone triggers (called by ZoneTrigger) ─────────────────────────────────

    public void OnEnterSmokeZone()
    {
        if (!smokeDamageEnabled) return;
        if (_inSmokeZone) return;

        float currentTemp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        Debug.LogWarning($"[Player] Oh no! You entered a smoke area! " +
                         $"Your temperature is {currentTemp:F1}°C and rising every {smokeTickInterval:F1}s...");

        ScoreTracker.Instance?.RegisterError();
        _inSmokeZone = true;
        SetVFX(smokeOverlayVFX, true);
        _smokeDamageCoroutine = StartCoroutine(SmokeDamageLoop());
    }

    public void OnExitSmokeZone()
    {
        if (!_inSmokeZone) return;

        float currentTemp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        Debug.Log($"[Player] You escaped the smoke area. Current temperature: {currentTemp:F1}°C.");

        _inSmokeZone = false;
        SetVFX(smokeOverlayVFX, false);

        if (_smokeDamageCoroutine != null)
        {
            StopCoroutine(_smokeDamageCoroutine);
            _smokeDamageCoroutine = null;
        }
    }

    public void OnEnterFireZone()
    {
        if (!fireDamageEnabled) return;

        float tempBefore = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        ScoreTracker.Instance?.RegisterError();
        TemperatureSystem.Instance.AddHeat(fireHeatOnEnter, "Fire Zone Entry");
        float tempAfter = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;

        Debug.LogWarning($"[Player] Oh no! You entered a fire area! " +
                         $"Your temperature spiked from {tempBefore:F1}°C to {tempAfter:F1}°C and keeps climbing!");

        PlayVFX(fireOverlayVFX);

        if (fireVFXDuration > 0f)
            Invoke(nameof(TurnOffFireVFX), fireVFXDuration);

        if (!_inFireZone)
        {
            _inFireZone = true;
            _fireDamageCoroutine = StartCoroutine(FireDamageLoop());
        }
    }

    public void OnExitFireZone()
    {
        float currentTemp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        Debug.Log($"[Player] You escaped the fire area. Current temperature: {currentTemp:F1}°C.");

        _inFireZone = false;
        SetVFX(fireOverlayVFX, false);

        if (_fireDamageCoroutine != null)
        {
            StopCoroutine(_fireDamageCoroutine);
            _fireDamageCoroutine = null;
        }
    }

    // ── Damage loops ──────────────────────────────────────────────────────────

    private IEnumerator SmokeDamageLoop()
    {
        while (_inSmokeZone)
        {
            yield return new WaitForSeconds(smokeTickInterval);
            if (!_inSmokeZone) break;

            TemperatureSystem.Instance.AddHeat(smokeHeatPerTick, "Smoke Zone");
            float currentTemp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
            Debug.LogWarning($"[Player] Still in the smoke... your temperature is now {currentTemp:F1}°C!");
        }
    }

    private IEnumerator FireDamageLoop()
    {
        while (_inFireZone)
        {
            yield return new WaitForSeconds(fireTickInterval);
            if (!_inFireZone) break;

            TemperatureSystem.Instance.AddHeat(fireHeatPerTick, "Fire Zone");
            float currentTemp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
            Debug.LogWarning($"[Player] Still in the fire! Your temperature is now {currentTemp:F1}°C!");
        }
    }

    // ── VFX helpers ───────────────────────────────────────────────────────────

    private void PlayVFX(GameObject vfx)
    {
        if (vfx == null) return;
        var ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
        else vfx.SetActive(true);
    }

    private void SetVFX(GameObject vfx, bool active)
    {
        if (vfx == null) return;
        vfx.SetActive(active);
    }

    private void TurnOffFireVFX() => SetVFX(fireOverlayVFX, false);
}