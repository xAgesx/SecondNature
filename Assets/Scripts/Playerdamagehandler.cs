using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// PlayerDamageHandler
// Attach to: XR Rig root (same object as TemperatureSystem)
//
// All damage types live here. Toggle each one on/off in the Inspector.
// To add a new damage type later: add a new [Header] block + handler method.
// ─────────────────────────────────────────────────────────────────────────────
public class PlayerDamageHandler : MonoBehaviour
{
    // ── Bare hand contact ─────────────────────────────────────────────────────

    [Header("Bare hand contact")]
    [Tooltip("Enable damage when bare hand touches a Door or Window tagged object.")]
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

    // ── Hand contact (called by hand colliders) ───────────────────────────────
    // Your hand GameObjects still need a Trigger Collider.
    // Add a small companion script (HandContactReporter) on each hand —
    // it just forwards the collision up to this script on the XR Rig.

    public void OnBareHandTouchedSurface()
    {
        if (!bareHandDamageEnabled) return;
        ScoreTracker.Instance?.RegisterError();          
        TemperatureSystem.Instance.AddHeat(bareHandHeat);
        PlayVFX(bareHandVFX);
    }

    // ── Zone triggers (called by ZoneTrigger on smoke/fire objects) ───────────

    public void OnEnterSmokeZone()
    {
        if (!smokeDamageEnabled) return;
        if (_inSmokeZone) return;
        ScoreTracker.Instance?.RegisterError();          
        _inSmokeZone = true;
        SetVFX(smokeOverlayVFX, true);
        _smokeDamageCoroutine = StartCoroutine(SmokeDamageLoop());
    }

    public void OnExitSmokeZone()
    {
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
        ScoreTracker.Instance?.RegisterError();
        TemperatureSystem.Instance.AddHeat(fireHeatOnEnter);
        PlayVFX(fireOverlayVFX);

        if (fireVFXDuration > 0f)
            Invoke(nameof(TurnOffFireVFX), fireVFXDuration);
    }

    public void OnExitFireZone()
    {
        SetVFX(fireOverlayVFX, false);
    }

    // ── Smoke loop ────────────────────────────────────────────────────────────

    private IEnumerator SmokeDamageLoop()
    {
        while (_inSmokeZone)
        {
            yield return new WaitForSeconds(smokeTickInterval);
            if (_inSmokeZone)
                TemperatureSystem.Instance.AddHeat(smokeHeatPerTick);
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