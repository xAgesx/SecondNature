using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// PlayerDamageHandler
// Attach to: XR Rig root (same object as TemperatureSystem + ProximityDetector)
//
// Handles all damage types:
//   • Bare hand contact  — called by DangerousInteractable
//   • Smoke proximity    — called by ProximityDetector
//   • Fire proximity     — called by ProximityDetector
//
// Assign Audio clips in the Inspector. The script manages its own AudioSources
// so each damage type can play independently without interrupting the others.
// ─────────────────────────────────────────────────────────────────────────────
public class PlayerDamageHandler : MonoBehaviour
{
    // ── Bare hand contact ─────────────────────────────────────────────────────

    [Header("Bare Hand Contact")]
    [Tooltip("Enable damage when a bare hand grabs a Door or Window interactable.")]
    public bool bareHandDamageEnabled = true;

    [Tooltip("Heat added on bare hand contact.")]
    public float bareHandHeat = 2f;

    [Tooltip("VFX GameObject on the XR Camera to play on bare hand damage.")]
    public GameObject bareHandVFX;

    [Tooltip("Sound clip played when the player touches a hot surface bare-handed (e.g. a yelp or pain grunt).")]
    public AudioClip bareHandSFX;

    // ── Smoke zone ────────────────────────────────────────────────────────────

    [Header("Smoke Zone")]
    [Tooltip("Enable damage over time while inside a smoke zone.")]
    public bool smokeDamageEnabled = true;

    [Tooltip("Heat added per tick inside smoke.")]
    public float smokeHeatPerTick = 1f;

    [Tooltip("Seconds between each smoke damage tick.")]
    public float smokeTickInterval = 3f;

    [Tooltip("VFX GameObject on the XR Camera to show while inside smoke.")]
    public GameObject smokeOverlayVFX;

    [Tooltip("Looping ambient sound while the player is inside smoke (e.g. coughing loop or smoke hiss).")]
    public AudioClip smokeLoopSFX;

    [Tooltip("One-shot cough clip played on each smoke damage tick.")]
    public AudioClip smokeCoughSFX;

    // ── Fire zone ─────────────────────────────────────────────────────────────

    [Header("Fire Zone")]
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

    [Tooltip("Looping crackling/roaring fire ambient sound while inside fire.")]
    public AudioClip fireLoopSFX;

    [Tooltip("One-shot pain/scream clip played on each fire damage tick.")]
    public AudioClip firePainSFX;

    // ── Audio settings ────────────────────────────────────────────────────────

    [Header("Audio Settings")]
    [Tooltip("Volume for all damage sound effects (0–1).")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Tooltip("Volume for looping ambient sounds (smoke loop, fire loop).")]
    [Range(0f, 1f)]
    public float ambientVolume = 0.6f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private bool _inSmokeZone = false;
    private bool _inFireZone = false;

    private Coroutine _smokeDamageCoroutine;
    private Coroutine _fireDamageCoroutine;

    // Two AudioSources: one for looping ambients, one for one-shot SFX
    private AudioSource _loopAudio;
    private AudioSource _sfxAudio;

    // Tracks which loop is currently playing so we can switch cleanly
    private enum LoopState { None, Smoke, Fire }
    private LoopState _currentLoop = LoopState.None;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Create two AudioSources on this GameObject rather than requiring manual setup
        _loopAudio = gameObject.AddComponent<AudioSource>();
        _loopAudio.loop = true;
        _loopAudio.playOnAwake = false;
        _loopAudio.spatialBlend = 0f; // 2-D — player hears it as internal sound
        _loopAudio.volume = ambientVolume;

        _sfxAudio = gameObject.AddComponent<AudioSource>();
        _sfxAudio.loop = false;
        _sfxAudio.playOnAwake = false;
        _sfxAudio.spatialBlend = 0f;
        _sfxAudio.volume = sfxVolume;
    }

    // ── Bare hand contact (called by DangerousInteractable) ───────────────────

    /// <param name="objectName">Display name of the grabbed object for debug output.</param>
    public void OnBareHandTouchedSurface(string objectName = "object")
    {
        if (!bareHandDamageEnabled) return;

        float tempBefore = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;

        ScoreTracker.Instance?.RegisterError();
        TemperatureSystem.Instance?.AddHeat(bareHandHeat, "Bare Hand Contact");

        float tempAfter = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;

        Debug.LogWarning($"[Player] You touched the {objectName} with bare hands! " +
                         $"Temperature: {tempBefore:F1}°C → {tempAfter:F1}°C");

        PlayOneShotSFX(bareHandSFX);
        PlayVFX(bareHandVFX);
    }

    // ── Zone callbacks (called by ProximityDetector) ──────────────────────────

    public void OnEnterSmokeZone()
    {
        if (!smokeDamageEnabled || _inSmokeZone) return;

        float temp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        Debug.LogWarning($"[Player] Entered smoke! Temperature: {temp:F1}°C — rising every {smokeTickInterval:F1}s.");

        ScoreTracker.Instance?.RegisterError();
        _inSmokeZone = true;

        SetVFX(smokeOverlayVFX, true);
        StartLoopSFX(smokeLoopSFX, LoopState.Smoke);

        _smokeDamageCoroutine = StartCoroutine(SmokeDamageLoop());
    }

    public void OnExitSmokeZone()
    {
        if (!_inSmokeZone) return;

        float temp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        Debug.Log($"[Player] Exited smoke. Temperature: {temp:F1}°C.");

        _inSmokeZone = false;

        SetVFX(smokeOverlayVFX, false);
        StopLoopSFX(LoopState.Smoke);

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
        TemperatureSystem.Instance?.AddHeat(fireHeatOnEnter, "Fire Zone Entry");

        float tempAfter = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;

        Debug.LogWarning($"[Player] Entered fire! Temperature spiked: {tempBefore:F1}°C → {tempAfter:F1}°C!");

        PlayOneShotSFX(firePainSFX);
        PlayVFX(fireOverlayVFX);

        if (fireVFXDuration > 0f)
            Invoke(nameof(TurnOffFireVFX), fireVFXDuration);

        if (!_inFireZone)
        {
            _inFireZone = true;
            StartLoopSFX(fireLoopSFX, LoopState.Fire);
            _fireDamageCoroutine = StartCoroutine(FireDamageLoop());
        }
    }

    public void OnExitFireZone()
    {
        if (!_inFireZone) return;

        float temp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        Debug.Log($"[Player] Exited fire. Temperature: {temp:F1}°C.");

        _inFireZone = false;

        SetVFX(fireOverlayVFX, false);
        StopLoopSFX(LoopState.Fire);

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

            TemperatureSystem.Instance?.AddHeat(smokeHeatPerTick, "Smoke Zone");
            float temp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
            Debug.LogWarning($"[Player] Still in smoke... temperature now {temp:F1}°C!");

            PlayOneShotSFX(smokeCoughSFX); // cough on each damage tick
        }
    }

    private IEnumerator FireDamageLoop()
    {
        while (_inFireZone)
        {
            yield return new WaitForSeconds(fireTickInterval);
            if (!_inFireZone) break;

            TemperatureSystem.Instance?.AddHeat(fireHeatPerTick, "Fire Zone");
            float temp = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
            Debug.LogWarning($"[Player] Still in fire! Temperature now {temp:F1}°C!");

            PlayOneShotSFX(firePainSFX); // pain sound on each tick
        }
    }

    // ── Audio helpers ─────────────────────────────────────────────────────────

    private void PlayOneShotSFX(AudioClip clip)
    {
        if (clip == null || _sfxAudio == null) return;
        _sfxAudio.PlayOneShot(clip, sfxVolume);
    }

    private void StartLoopSFX(AudioClip clip, LoopState state)
    {
        if (clip == null || _loopAudio == null) return;
        if (_currentLoop == state) return; // already playing this loop

        _loopAudio.Stop();
        _loopAudio.clip = clip;
        _loopAudio.volume = ambientVolume;
        _loopAudio.Play();
        _currentLoop = state;
    }

    private void StopLoopSFX(LoopState state)
    {
        if (_loopAudio == null || _currentLoop != state) return;
        _loopAudio.Stop();
        _currentLoop = LoopState.None;
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