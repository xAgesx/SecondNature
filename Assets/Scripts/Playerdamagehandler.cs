using System.Collections;
using UnityEngine;

public class PlayerDamageHandler : MonoBehaviour
{
    [Header("Bare Hand Contact")]
    public bool bareHandDamageEnabled = true;
    public float bareHandHeat = 2f;
    public GameObject bareHandVFX;
    public AudioClip bareHandSFX;

    [Header("Smoke Zone")]
    public bool smokeDamageEnabled = true;
    public float smokeHeatPerTick = 1f;
    public float smokeTickInterval = 3f;
    public GameObject smokeOverlayVFX;
    public AudioClip smokeLoopSFX;
    public AudioClip smokeCoughSFX;

    [Header("Fire Zone")]
    public bool fireDamageEnabled = true;
    public float fireHeatOnEnter = 5f;
    public float fireHeatPerTick = 2f;
    public float fireTickInterval = 2f;
    public GameObject fireOverlayVFX;
    public float fireVFXDuration = 1.5f;
    public AudioClip fireLoopSFX;
    public AudioClip firePainSFX;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 0.6f;

    private bool _inSmokeZone = false;
    private bool _inFireZone = false;
    private Coroutine _smokeDamageCoroutine;
    private Coroutine _fireDamageCoroutine;
    private AudioSource _loopAudio;
    private AudioSource _sfxAudio;
    private enum LoopState { None, Smoke, Fire }
    private LoopState _currentLoop = LoopState.None;

    private void Awake()
    {
        _loopAudio = gameObject.AddComponent<AudioSource>();
        _loopAudio.loop = true;
        _loopAudio.playOnAwake = false;
        _loopAudio.spatialBlend = 0f;
        _loopAudio.volume = ambientVolume;

        _sfxAudio = gameObject.AddComponent<AudioSource>();
        _sfxAudio.loop = false;
        _sfxAudio.playOnAwake = false;
        _sfxAudio.spatialBlend = 0f;
        _sfxAudio.volume = sfxVolume;
    }

    public void OnBareHandTouchedSurface(string objectName = "object")
    {
        if (!bareHandDamageEnabled) return;
        TemperatureSystem.Instance?.AddHeat(bareHandHeat, "Bare Hand Contact");
        Debug.LogWarning($"[PlayerDamageHandler] Bare hand touched {objectName}!");
        PlayOneShotSFX(bareHandSFX);
        PlayVFX(bareHandVFX);
    }

    public void OnEnterSmokeZone()
    {
        if (!smokeDamageEnabled || _inSmokeZone) return;
        _inSmokeZone = true;
        SetVFX(smokeOverlayVFX, true);
        StartLoopSFX(smokeLoopSFX, LoopState.Smoke);
        _smokeDamageCoroutine = StartCoroutine(SmokeDamageLoop());
        Debug.LogWarning("[PlayerDamageHandler] Entered smoke zone.");
    }

    public void OnExitSmokeZone()
    {
        if (!_inSmokeZone) return;
        _inSmokeZone = false;
        SetVFX(smokeOverlayVFX, false);
        StopLoopSFX(LoopState.Smoke);
        if (_smokeDamageCoroutine != null) { StopCoroutine(_smokeDamageCoroutine); _smokeDamageCoroutine = null; }
        Debug.Log("[PlayerDamageHandler] Exited smoke zone.");
    }

    public void OnEnterFireZone()
    {
        if (!fireDamageEnabled) return;
        TemperatureSystem.Instance?.AddHeat(fireHeatOnEnter, "Fire Zone Entry");
        PlayOneShotSFX(firePainSFX);
        PlayVFX(fireOverlayVFX);
        if (fireVFXDuration > 0f) Invoke(nameof(TurnOffFireVFX), fireVFXDuration);
        if (!_inFireZone)
        {
            _inFireZone = true; // set before StartLoopSFX so state is correct
            StartLoopSFX(fireLoopSFX, LoopState.Fire);
            _fireDamageCoroutine = StartCoroutine(FireDamageLoop());
        }
        Debug.LogWarning("[PlayerDamageHandler] Entered fire zone.");
    }

    public void OnExitFireZone()
    {
        if (!_inFireZone) return;
        _inFireZone = false;
        SetVFX(fireOverlayVFX, false);
        StopLoopSFX(LoopState.Fire);
        if (_fireDamageCoroutine != null) { StopCoroutine(_fireDamageCoroutine); _fireDamageCoroutine = null; }
        Debug.Log("[PlayerDamageHandler] Exited fire zone.");
    }

    private IEnumerator SmokeDamageLoop()
    {
        while (_inSmokeZone)
        {
            yield return new WaitForSeconds(smokeTickInterval);
            if (!_inSmokeZone) break;
            TemperatureSystem.Instance?.AddHeat(smokeHeatPerTick, "Smoke Zone");
            PlayOneShotSFX(smokeCoughSFX);
        }
    }

    private IEnumerator FireDamageLoop()
    {
        while (_inFireZone)
        {
            yield return new WaitForSeconds(fireTickInterval);
            if (!_inFireZone) break;
            TemperatureSystem.Instance?.AddHeat(fireHeatPerTick, "Fire Zone");
            PlayOneShotSFX(firePainSFX);
        }
    }

    private void PlayOneShotSFX(AudioClip clip) { if (clip && _sfxAudio) _sfxAudio.PlayOneShot(clip, sfxVolume); }

    private void StartLoopSFX(AudioClip clip, LoopState state)
    {
        if (!clip || !_loopAudio) return;
        // Allow restart if same state but audio has stopped (e.g. after exiting smoke back into fire)
        if (_currentLoop == state && _loopAudio.isPlaying) return;
        _loopAudio.Stop();
        _loopAudio.clip = clip;
        _loopAudio.volume = ambientVolume;
        _loopAudio.Play();
        _currentLoop = state;
    }

    private void StopLoopSFX(LoopState state) { if (_loopAudio && _currentLoop == state) { _loopAudio.Stop(); _currentLoop = LoopState.None; } }
    private void PlayVFX(GameObject vfx) { if (!vfx) return; var ps = vfx.GetComponent<ParticleSystem>(); if (ps) ps.Play(); else vfx.SetActive(true); }
    private void SetVFX(GameObject vfx, bool active) { if (vfx) vfx.SetActive(active); }
    private void TurnOffFireVFX() => SetVFX(fireOverlayVFX, false);
}