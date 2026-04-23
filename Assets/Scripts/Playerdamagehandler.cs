using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// PlayerDamageHandler
// Attach to: XR Rig root (same object as TemperatureSystem)
//
// All damage types live here. Toggle each one on/off in the Inspector.
// Hand contact is now driven by DangerousInteractable on Door/Window objects
// — no hand colliders needed.
// ─────────────────────────────────────────────────────────────────────────────
public class PlayerDamageHandler : MonoBehaviour
{
    // ── Bare hand contact ─────────────────────────────────────────────────────

    [Header("Bare hand contact")]
    [Tooltip("Enable damage when a bare hand hovers over a Door or Window interactable.")]
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

    [Tooltip("VFX GameObject on the XR Camera to play on fire entry.")]
    public GameObject fireOverlayVFX;

    [Tooltip("How long the fire VFX stays on after entering (seconds). 0 = until exit.")]
    public float fireVFXDuration = 1.5f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private bool _inSmokeZone = false;
    private Coroutine _smokeDamageCoroutine;

    // ── Bare hand contact (called by DangerousInteractable on Door/Window) ────

    public void OnBareHandTouchedSurface()
    {
        if (!bareHandDamageEnabled)
        {
            Debug.Log("[PlayerDamageHandler] Bare hand contact detected but damage is DISABLED.");
            return;
        }

        Debug.Log("[PlayerDamageHandler] 🖐️ Bare hand touched dangerous surface — dealing damage.");
        ScoreTracker.Instance?.RegisterError();
        TemperatureSystem.Instance.AddHeat(bareHandHeat, "Bare Hand Contact");
        PlayVFX(bareHandVFX);
    }

    // ── Zone triggers (called by ZoneTrigger on smoke/fire objects) ───────────

    public void OnEnterSmokeZone()
    {
        if (!smokeDamageEnabled)
        {
            Debug.Log("[PlayerDamageHandler] Entered smoke zone but smoke damage is DISABLED.");
            return;
        }

        if (_inSmokeZone)
        {
            Debug.Log("[PlayerDamageHandler] Entered smoke zone but already tracking one — ignoring.");
            return;
        }

        Debug.Log("[PlayerDamageHandler] 💨 Entered SMOKE zone — starting damage loop " +
                  $"({smokeHeatPerTick:F1}°C every {smokeTickInterval:F1}s).");

        ScoreTracker.Instance?.RegisterError();
        _inSmokeZone = true;
        SetVFX(smokeOverlayVFX, true);
        _smokeDamageCoroutine = StartCoroutine(SmokeDamageLoop());
    }

    public void OnExitSmokeZone()
    {
        Debug.Log("[PlayerDamageHandler] 💨 Exited SMOKE zone — damage loop stopped.");
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
        if (!fireDamageEnabled)
        {
            Debug.Log("[PlayerDamageHandler] Entered fire zone but fire damage is DISABLED.");
            return;
        }

        Debug.Log($"[PlayerDamageHandler] 🔥 Entered FIRE zone — dealing {fireHeatOnEnter:F1}°C instantly.");
        ScoreTracker.Instance?.RegisterError();
        TemperatureSystem.Instance.AddHeat(fireHeatOnEnter, "Fire Zone");
        PlayVFX(fireOverlayVFX);

        if (fireVFXDuration > 0f)
            Invoke(nameof(TurnOffFireVFX), fireVFXDuration);
    }

    public void OnExitFireZone()
    {
        Debug.Log("[PlayerDamageHandler] 🔥 Exited FIRE zone.");
        SetVFX(fireOverlayVFX, false);
    }

    // ── Smoke loop ────────────────────────────────────────────────────────────

    private IEnumerator SmokeDamageLoop()
    {
        while (_inSmokeZone)
        {
            yield return new WaitForSeconds(smokeTickInterval);
            if (_inSmokeZone)
            {
                Debug.Log($"[PlayerDamageHandler] 💨 Smoke tick — dealing {smokeHeatPerTick:F1}°C.");
                TemperatureSystem.Instance.AddHeat(smokeHeatPerTick, "Smoke Zone (Tick)");
            }
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