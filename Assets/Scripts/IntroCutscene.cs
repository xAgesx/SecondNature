using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class IntroCutscene : MonoBehaviour
{
    [Header("Waypoints (GoHere objects, auto-found)")]
    public string waypointPrefix = "GoHere";
    public int deskWaypointIndex = 4;
    public int doorHitWaypointIndex = 8;

    [Header("Door")]
    public Transform doorLeaf;
    public float doorOpenAngle = 90f;
    public float doorOpenDuration = 1.5f;
    public float doorCloseDuration = 0.4f;

    [Header("Vibrant PostFX")]
    public Volume cutsceneVolume;
    [Range(0f, 2f)] public float saturation = 1.2f;
    [Range(0f, 2f)] public float bloomIntensity = 1.5f;
    [Range(0f, 1f)] public float bloomScatter = 0.4f;
    [Range(-2f, 2f)] public float exposure = 0.8f;
    [Range(-100f, 100f)] public float contrast = 30f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource ambienceSource;
    public AudioClip fireAlarmClip;
    public AudioClip headHitClip;
    public AudioClip doorSlamClip;
    public AudioClip ambienceClip;
    public AudioClip footstepClip;
    public AudioClip heartbeatClip;

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;
    public CanvasGroup blinkCanvasGroup;

    [Header("Cutscene Lighting")]
    public Color cutsceneAmbient = new Color(0.5f, 0.45f, 0.4f);
    public Color cutsceneFogColor = new Color(0.6f, 0.55f, 0.5f);
    public float cutsceneFogDensity = 0.008f;

    [Header("Sit")]
    [Range(0f, 0.5f)]
    public float sitHeight = 0.2f;
    public float sitDuration = 0.7f;
    public float standDuration = 0.3f;
    public float sitPause = 3f;

    [Header("Movement")]
    public float walkSpeed = 1.8f;
    public float runSpeed = 3.5f;
    public float rotationSpeed = 3.5f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Bob")]
    public float walkFreq = 1.8f;
    public float walkAmpV = 0.02f;
    public float walkAmpH = 0.008f;
    public float runFreq = 2.8f;
    public float runAmpV = 0.035f;
    public float runAmpH = 0.012f;

    [Header("Alarm Effects")]
    public Color alarmFlashColor = new Color(1f, 0.15f, 0.05f);
    public float alarmFlashSpeed = 4f;

    [Header("Dizziness")]
    public float dizzySwayAmp = 0.02f;
    public float dizzySwayFreq = 1.5f;

    [Header("Timing")]
    public float alarmDuration = 3f;
    public float shakeDuration = 0.5f;
    public float fadeIn = 1.5f;
    public float fadeOut = 1f;
    public float blackHold = 2f;

    private Transform _playerRoot;
    private Transform _camOffset;
    private Camera _playerCam;
    private LocomotionSystem _locomotion;
    private CharacterController _charCtrl;
    private VolumeProfile _rtProfile;
    private Vector3 _offsetOrigin;
    private bool _playing;

    // state saving
    private List<Light> _lights = new List<Light>();
    private List<Volume> _volumes = new List<Volume>();
    private List<ParticleSystem> _particles = new List<ParticleSystem>();
    private bool[] _lightState;
    private bool[] _volState;
    private bool[] _partState;
    private bool _camPost;
    private bool _fogState;
    private Color _fogColor;
    private float _fogDensity;
    private Color _ambientLight;
    private AmbientMode _ambientMode;
    private Light _dirLight;
    private bool _dirLightState;
    private Color _dirColor;
    private float _dirIntensity;
    private XRInteractionManager _xrManager;
    private bool _xrManagerState;

    // runtime postfx handles
    private Vignette _vignette;
    private ChromaticAberration _chroma;
    private ColorAdjustments _colorAdj;
    private float _baseVignette;

    private void Awake()
    {
        _playerCam = Camera.main;
        if (_playerCam != null)
        {
            _playerRoot = _playerCam.transform.root;
            _camOffset = _playerCam.transform.parent;
        }
        _locomotion = FindObjectOfType<LocomotionSystem>();
        _charCtrl = _playerRoot != null ? _playerRoot.GetComponent<CharacterController>() : null;
        _xrManager = FindObjectOfType<XRInteractionManager>();
    }

    private IEnumerator Start()
    {
        SetupDefaults();
        yield return null;
        if (_camOffset != null) _offsetOrigin = _camOffset.localPosition;
        if (doorLeaf == null) doorLeaf = FindDoorLeaf();
        BeginCutscene();
    }

    private void SetupDefaults()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
            sfxSource.playOnAwake = false;
        }
        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.spatialBlend = 0f;
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.volume = 0.5f;
        }
        if (fadeCanvasGroup == null) MakeFadeCanvas();
        if (blinkCanvasGroup == null) MakeBlinkCanvas();
        if (cutsceneVolume == null) MakeVolume();
    }

    // ── WAYPOINTS ──

    private Transform[] FindWaypoints()
    {
        var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        var filtered = new List<(Transform t, int idx)>();

        foreach (var t in all)
        {
            if (!t.gameObject.activeInHierarchy) continue;
            string name = t.name.Trim();
            if (name.Length == 0) continue;

            string lower = name.ToLowerInvariant();
            if (!lower.StartsWith("gohere")) continue;

            int idx = 0;
            string rest = name.Substring("gohere".Length).Trim();
            rest = rest.TrimStart('(').TrimEnd(')').Trim();
            if (rest.Length > 0 && int.TryParse(rest, out int n)) idx = n;

            filtered.Add((t, idx));
        }

        filtered.Sort((a, b) => a.idx.CompareTo(b.idx));
        var result = filtered.Select(f => f.t).ToArray();

        Debug.Log($"[IntroCutscene] Found {result.Length} waypoints: " +
            string.Join(", ", result.Select(t => $"{t.name}")));

        return result;
    }

    private Transform[] _waypoints;
    private Transform WP(int i) => i >= 0 && i < _waypoints.Length ? _waypoints[i] : null;

    // ── DOOR ──

    private Transform FindDoorLeaf()
    {
        GameObject[] doors = GameObject.FindGameObjectsWithTag("Door");
        foreach (var door in doors)
        {
            Transform t = door.transform;
            Transform leaf = t.Find("door01") ?? t.Find("door01_L") ?? t.Find("Door01") ?? t.Find("door01_R");
            if (leaf != null) return leaf;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                if (c.name.ToLower().Contains("door") && !c.name.ToLower().Contains("frame"))
                    return c;
            }
        }
        if (doors.Length > 0) return doors[0].transform;
        return null;
    }

    private IEnumerator OpenDoor()
    {
        if (doorLeaf == null) yield break;
        Quaternion s = doorLeaf.localRotation;
        Quaternion e = Quaternion.Euler(0f, doorOpenAngle, 0f);
        float t = 0f;
        while (t < doorOpenDuration)
        {
            t += Time.deltaTime;
            doorLeaf.localRotation = Quaternion.Lerp(s, e, Mathf.SmoothStep(0f, 1f, t / doorOpenDuration));
            yield return null;
        }
        doorLeaf.localRotation = e;
    }

    private IEnumerator SlamDoor()
    {
        if (doorLeaf == null) yield break;
        if (doorSlamClip != null && sfxSource != null) sfxSource.PlayOneShot(doorSlamClip);
        Quaternion s = doorLeaf.localRotation;
        Quaternion e = Quaternion.identity;
        float t = 0f;
        while (t < doorCloseDuration)
        {
            t += Time.deltaTime;
            float p = t / doorCloseDuration;
            doorLeaf.localRotation = Quaternion.Lerp(s, e, p * p);
            yield return null;
        }
        doorLeaf.localRotation = e;
    }

    // ── CUTSCENE ──

    public void BeginCutscene()
    {
        if (_playing) return;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        _playing = true;
        DisableAllControls();

        _waypoints = FindWaypoints();
        if (_waypoints.Length == 0) { Debug.LogError("[IntroCutscene] No waypoints found!"); yield break; }

        SaveWorldState();
        ApplyCutsceneAmbiance();

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.gameObject.SetActive(true);
        }

        Volume vol = cutsceneVolume;
        SetupVibrant(vol);
        CachePostFxHandles(vol);

        if (WP(0) != null && _playerRoot != null)
        {
            _playerRoot.position = WP(0).position;
            _playerRoot.rotation = WP(0).rotation;
        }

        yield return StartCoroutine(Blink());
        yield return StartCoroutine(Fade(1f, 0f, fadeIn));

        // Walk to desk
        int deskIdx = Mathf.Clamp(deskWaypointIndex, 1, _waypoints.Length - 1);
        for (int i = 1; i <= deskIdx; i++)
        {
            if (WP(i) == null) continue;
            float speed = (i == deskIdx) ? walkSpeed * 0.7f : walkSpeed;
            float freq = (i == deskIdx) ? walkFreq * 0.8f : walkFreq;
            yield return StartCoroutine(WalkTo(WP(i).position, speed, freq, walkAmpV, walkAmpH));
            if (i == deskIdx) yield return StartCoroutine(OpenDoor());
        }

        // Sit
        yield return StartCoroutine(Sit());
        if (vol != null) vol.weight = 1f;
        yield return StartCoroutine(SitBreathing(sitPause));

        // Alarm
        if (fireAlarmClip != null && sfxSource != null) sfxSource.PlayOneShot(fireAlarmClip);
        yield return StartCoroutine(AlarmEffect(alarmDuration * 0.5f));

        // Stand
        yield return StartCoroutine(Stand());
        yield return new WaitForSeconds(alarmDuration * 0.2f);
        if (vol != null) vol.weight = 0f;

        // Run to door-hit
        int hitIdx = Mathf.Clamp(doorHitWaypointIndex, deskIdx + 1, _waypoints.Length - 1);
        for (int i = deskIdx + 1; i <= hitIdx; i++)
        {
            if (WP(i) == null) continue;
            yield return StartCoroutine(WalkTo(WP(i).position, runSpeed, runFreq, runAmpV, runAmpH));
        }

        // Door slam + hit
        if (doorLeaf != null) yield return StartCoroutine(SlamDoor());
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(HitEffect());

        // Fade black
        yield return StartCoroutine(Dizziness(1.5f));
        yield return StartCoroutine(Fade(0f, 1f, fadeOut));
        if (vol != null) vol.weight = 0f;
        yield return StartCoroutine(Blink());

        if (WP(0) != null && _playerRoot != null)
        {
            _playerRoot.SetPositionAndRotation(WP(0).position, WP(0).rotation);
            if (_camOffset != null) _camOffset.localPosition = _offsetOrigin;
        }
        yield return new WaitForSeconds(blackHold);

        RestoreWorldState();
        yield return StartCoroutine(Fade(1f, 0f, fadeIn));
        if (fadeCanvasGroup != null) fadeCanvasGroup.gameObject.SetActive(false);

        EnableAllControls();
        _playing = false;
    }

    // ── CONTROLS ──

    private void DisableAllControls()
    {
        if (_locomotion != null) _locomotion.enabled = false;
        if (_charCtrl != null) _charCtrl.enabled = false;
        var tp = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
        if (tp != null) tp.enabled = false;
        if (_xrManager != null) { _xrManagerState = _xrManager.enabled; _xrManager.enabled = false; }
        var interactors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(FindObjectsSortMode.None);
        foreach (var inter in interactors) if (inter != null) inter.enabled = false;
        var actionManager = FindObjectOfType<UnityEngine.InputSystem.InputActionAsset>();
        if (actionManager != null) actionManager.Disable();
    }

    private void EnableAllControls()
    {
        if (_locomotion != null) _locomotion.enabled = true;
        if (_charCtrl != null) _charCtrl.enabled = true;
        var tp = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>();
        if (tp != null) tp.enabled = true;
        if (_xrManager != null) _xrManager.enabled = _xrManagerState;
        var interactors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(FindObjectsSortMode.None);
        foreach (var inter in interactors) if (inter != null) inter.enabled = true;
        var actionManager = FindObjectOfType<UnityEngine.InputSystem.InputActionAsset>();
        if (actionManager != null) actionManager.Enable();
    }

    // ── AMBIANCE ──

    private void SaveWorldState()
    {
        _lights.Clear(); _volumes.Clear(); _particles.Clear();
        _lights.AddRange(FindObjectsOfType<Light>(true));
        _volumes.AddRange(FindObjectsOfType<Volume>(true));
        _particles.AddRange(FindObjectsOfType<ParticleSystem>(true));

        _lightState = new bool[_lights.Count];
        _volState = new bool[_volumes.Count];
        _partState = new bool[_particles.Count];
        for (int i = 0; i < _lights.Count; i++) _lightState[i] = _lights[i].enabled;
        for (int i = 0; i < _volumes.Count; i++) _volState[i] = _volumes[i].enabled;
        for (int i = 0; i < _particles.Count; i++) _partState[i] = _particles[i].isPlaying;

        _fogState = RenderSettings.fog;
        _fogColor = RenderSettings.fogColor;
        _fogDensity = RenderSettings.fogDensity;
        _ambientLight = RenderSettings.ambientLight;
        _ambientMode = RenderSettings.ambientMode;

        _dirLight = FindObjectOfType<Light>(true);
        if (_dirLight != null && _dirLight.type == LightType.Directional)
        {
            _dirLightState = _dirLight.enabled;
            _dirColor = _dirLight.color;
            _dirIntensity = _dirLight.intensity;
        }

        if (_playerCam != null)
        {
            var cd = _playerCam.GetComponent<UniversalAdditionalCameraData>();
            if (cd == null) cd = _playerCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            _camPost = cd.renderPostProcessing;
        }
    }

    private void ApplyCutsceneAmbiance()
    {
        foreach (var l in _lights) if (l != null) l.enabled = false;
        foreach (var v in _volumes) if (v != null && v != cutsceneVolume) v.enabled = false;
        foreach (var p in _particles) if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (_playerCam != null)
        {
            var cd = _playerCam.GetComponent<UniversalAdditionalCameraData>();
            if (cd == null) cd = _playerCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cd.renderPostProcessing = true;
            cd.volumeLayerMask = LayerMask.GetMask("Default");
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = cutsceneFogColor;
        RenderSettings.fogDensity = cutsceneFogDensity;
        RenderSettings.fogMode = FogMode.Exponential;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = cutsceneAmbient;
        RenderSettings.ambientIntensity = 1.0f;

        if (_dirLight != null)
        {
            _dirLight.enabled = true;
            _dirLight.color = new Color(1f, 0.9f, 0.8f);
            _dirLight.intensity = 0.8f;
        }

        if (ambienceClip != null && ambienceSource != null)
        {
            ambienceSource.clip = ambienceClip;
            ambienceSource.Play();
        }
    }

    private void RestoreWorldState()
    {
        for (int i = 0; i < _lights.Count; i++) if (_lights[i] != null) _lights[i].enabled = _lightState[i];
        for (int i = 0; i < _volumes.Count; i++) if (_volumes[i] != null) _volumes[i].enabled = _volState[i];
        for (int i = 0; i < _particles.Count; i++) if (_particles[i] != null && _partState[i]) _particles[i].Play();

        RenderSettings.fog = _fogState;
        RenderSettings.fogColor = _fogColor;
        RenderSettings.fogDensity = _fogDensity;
        RenderSettings.ambientMode = _ambientMode;
        RenderSettings.ambientLight = _ambientLight;

        if (_dirLight != null) _dirLight.enabled = _dirLightState;
        if (_dirLight != null) _dirLight.color = _dirColor;
        if (_dirLight != null) _dirLight.intensity = _dirIntensity;

        if (ambienceSource != null) ambienceSource.Stop();

        if (_playerCam != null)
        {
            var cd = _playerCam.GetComponent<UniversalAdditionalCameraData>();
            if (cd != null) cd.renderPostProcessing = _camPost;
        }
    }

    // ── MOVEMENT (synchronized position + rotation) ──

    private IEnumerator WalkTo(Vector3 target, float speed, float freq, float ampV, float ampH)
    {
        if (_playerRoot == null) yield break;
        Vector3 startPos = _playerRoot.position;
        Quaternion startRot = _playerRoot.rotation;
        float dist = Vector3.Distance(startPos, target);
        if (dist < 0.05f) yield break;
        float dur = dist / Mathf.Max(speed, 0.01f);
        float t = 0f;

        Vector3 dir = target - startPos; dir.y = 0f;
        Quaternion endRot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir) : startRot;

        while (t < dur)
        {
            t += Time.deltaTime;
            float raw = Mathf.Clamp01(t / dur);
            float p = moveCurve.Evaluate(raw);

            _playerRoot.position = Vector3.LerpUnclamped(startPos, target, p);
            _playerRoot.rotation = Quaternion.Slerp(startRot, endRot, Mathf.SmoothStep(0f, 1f, raw));

            HeadBob(t, freq, ampV, ampH);
            yield return null;
        }
        _playerRoot.position = target;
        _playerRoot.rotation = endRot;
        if (_camOffset != null) _camOffset.localPosition = _offsetOrigin;
    }

    private void HeadBob(float t, float freq, float ampV, float ampH)
    {
        if (_camOffset == null) return;
        float bV = Mathf.Sin(t * freq * Mathf.PI * 2f) * ampV;
        float bH = Mathf.Sin(t * freq * Mathf.PI * 1f + 0.5f) * ampH;
        _camOffset.localPosition = _offsetOrigin + new Vector3(bH, bV, 0f);
    }

    // ── SIT / STAND ──

    private IEnumerator Sit()
    {
        if (_camOffset == null) yield break;
        Vector3 from = _camOffset.localPosition;
        Vector3 to = _offsetOrigin + new Vector3(0f, -sitHeight, 0f);
        float t = 0f;
        while (t < sitDuration)
        {
            t += Time.deltaTime;
            _camOffset.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / sitDuration));
            yield return null;
        }
        _camOffset.localPosition = to;
    }

    private IEnumerator Stand()
    {
        if (_camOffset == null) yield break;
        Vector3 from = _camOffset.localPosition;
        float t = 0f;
        while (t < standDuration)
        {
            t += Time.deltaTime;
            float p = t / standDuration;
            _camOffset.localPosition = Vector3.Lerp(from, _offsetOrigin, Mathf.SmoothStep(0f, 1f, p));
            yield return null;
        }
        _camOffset.localPosition = _offsetOrigin;
    }

    private IEnumerator SitBreathing(float duration)
    {
        if (_camOffset == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float breath = Mathf.Sin(t * 1.8f * Mathf.PI * 2f) * 0.003f;
            Vector3 bases = _offsetOrigin + new Vector3(0f, -sitHeight, 0f);
            _camOffset.localPosition = bases + new Vector3(0f, breath, 0f);
            yield return null;
        }
        _camOffset.localPosition = _offsetOrigin + new Vector3(0f, -sitHeight, 0f);
    }

    // ── EFFECTS ──

    private IEnumerator Blink()
    {
        if (blinkCanvasGroup == null) yield break;
        blinkCanvasGroup.gameObject.SetActive(true);

        float t = 0f;
        while (t < 0.08f) { t += Time.deltaTime; blinkCanvasGroup.alpha = t / 0.08f; yield return null; }
        blinkCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(0.06f);
        t = 0f;
        while (t < 0.1f) { t += Time.deltaTime; blinkCanvasGroup.alpha = 1f - t / 0.1f; yield return null; }
        blinkCanvasGroup.alpha = 0f;

        blinkCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator AlarmEffect(float duration)
    {
        if (_vignette == null && _chroma == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float pulse = Mathf.PingPong(t * alarmFlashSpeed, 1f);
            float easedPulse = Mathf.SmoothStep(0f, 1f, pulse);

            if (_vignette != null)
            {
                _vignette.intensity.value = _baseVignette + easedPulse * 0.35f;
                _vignette.color.value = Color.Lerp(Color.black, alarmFlashColor, easedPulse * 0.5f);
            }
            if (_chroma != null)
            {
                _chroma.intensity.value = easedPulse * 0.25f;
            }
            yield return null;
        }
        if (_vignette != null) { _vignette.intensity.value = _baseVignette; _vignette.color.value = Color.black; }
        if (_chroma != null) _chroma.intensity.value = 0f;
    }

    private IEnumerator HitEffect()
    {
        if (_playerRoot == null || _camOffset == null) yield break;
        Vector3 rootO = _playerRoot.position;
        Vector3 offO = _camOffset.localPosition;
        float t = 0f;

        if (_chroma != null) _chroma.intensity.value = 0.5f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float m = Mathf.Lerp(0.15f, 0f, t / shakeDuration);
            Vector3 r = Random.insideUnitSphere * m; r.y = 0f;
            _playerRoot.position = rootO + r;
            if (headHitClip != null && sfxSource != null && t < 0.05f) sfxSource.PlayOneShot(headHitClip);
            yield return null;
        }
        _playerRoot.position = rootO;

        float fallDur = 0.6f;
        float fallDist = 0.5f;
        Vector3 fallPos = rootO - _playerRoot.forward * fallDist + Vector3.up * -0.15f;
        Vector3 fallOff = offO + new Vector3(0f, -0.4f, 0f);
        t = 0f;
        while (t < fallDur)
        {
            t += Time.deltaTime;
            float p = t / fallDur;
            _playerRoot.position = Vector3.Lerp(rootO, fallPos, p * p);
            _camOffset.localPosition = Vector3.Lerp(offO, fallOff, p * p);
            yield return null;
        }
        _playerRoot.position = fallPos;
        _camOffset.localPosition = fallOff;

        if (_chroma != null) _chroma.intensity.value = 0f;
    }

    private IEnumerator Dizziness(float duration)
    {
        if (_camOffset == null) yield break;
        Vector3 baseOffset = _camOffset.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float decay = 1f - Mathf.Clamp01(t / duration);
            float swayX = Mathf.Sin(t * dizzySwayFreq * Mathf.PI * 2f) * dizzySwayAmp * decay;
            float swayZ = Mathf.Cos(t * dizzySwayFreq * Mathf.PI * 1.7f) * dizzySwayAmp * decay;
            _camOffset.localPosition = baseOffset + new Vector3(swayX, 0f, swayZ);
            yield return null;
        }
        _camOffset.localPosition = baseOffset;
    }

    // ── POSTFX ──

    private void CachePostFxHandles(Volume vol)
    {
        if (vol == null || vol.profile == null) return;
        VolumeProfile p = vol.profile;
        p.TryGet(out _vignette);
        p.TryGet(out _chroma);
        p.TryGet(out _colorAdj);
        _baseVignette = _vignette != null ? _vignette.intensity.value : 0f;
    }

    private void SetupVibrant(Volume vol)
    {
        if (vol == null) return;
        VolumeProfile p = vol.profile;
        if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); vol.profile = p; _rtProfile = p; }

        ColorAdjustments ca;
        p.TryGet(out ca); if (ca == null) ca = p.Add<ColorAdjustments>(true);
        ca.postExposure.value = exposure;
        ca.postExposure.overrideState = true;
        ca.contrast.value = contrast;
        ca.contrast.overrideState = true;
        ca.saturation.value = saturation * 100f;
        ca.saturation.overrideState = true;
        ca.colorFilter.value = new Color(1f, 0.92f, 0.85f);
        ca.colorFilter.overrideState = true;

        Bloom bl;
        p.TryGet(out bl); if (bl == null) bl = p.Add<Bloom>(true);
        bl.threshold.value = 0.5f;
        bl.threshold.overrideState = true;
        bl.intensity.value = bloomIntensity;
        bl.intensity.overrideState = true;
        bl.scatter.value = bloomScatter;
        bl.scatter.overrideState = true;
        bl.tint.value = new Color(1f, 0.85f, 0.5f);
        bl.tint.overrideState = true;
        bl.dirtTexture.value = null;
        bl.dirtTexture.overrideState = false;

        Tonemapping tn;
        p.TryGet(out tn); if (tn == null) tn = p.Add<Tonemapping>(true);
        tn.mode.value = TonemappingMode.ACES;
        tn.mode.overrideState = true;

        WhiteBalance wb;
        p.TryGet(out wb); if (wb == null) wb = p.Add<WhiteBalance>(true);
        wb.temperature.value = 30f;
        wb.temperature.overrideState = true;

        p.TryGet(out _vignette); if (_vignette == null) _vignette = p.Add<Vignette>(true);
        _vignette.intensity.value = 0.2f;
        _vignette.intensity.overrideState = true;
        _vignette.smoothness.value = 0.35f;
        _vignette.smoothness.overrideState = true;
        _vignette.color.value = Color.black;
        _vignette.color.overrideState = true;
        _baseVignette = 0.2f;

        p.TryGet(out _chroma); if (_chroma == null) _chroma = p.Add<ChromaticAberration>(true);
        _chroma.intensity.value = 0f;
        _chroma.intensity.overrideState = true;

        LiftGammaGain lg;
        p.TryGet(out lg); if (lg == null) lg = p.Add<LiftGammaGain>(true);
        lg.lift.value = new Vector4(0.03f, 0.02f, 0f, 0f);
        lg.lift.overrideState = true;

        ShadowsMidtonesHighlights smh;
        p.TryGet(out smh); if (smh == null) smh = p.Add<ShadowsMidtonesHighlights>(true);
        smh.shadows.value = new Vector4(1f, 0.8f, 0.5f, 0.5f);
        smh.shadows.overrideState = true;
        smh.highlights.value = new Vector4(1f, 1f, 1f, 1f);
        smh.highlights.overrideState = true;

        p.TryGet(out _colorAdj);
    }

    // ── HELPERS ──

    private void MakeFadeCanvas()
    {
        GameObject go = new GameObject("CutsceneFadeCanvas");
        go.transform.SetParent(transform); go.SetActive(false);
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 999;
        fadeCanvasGroup = go.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 1f; fadeCanvasGroup.blocksRaycasts = true; fadeCanvasGroup.interactable = true;
        GameObject imgGo = new GameObject("BlackOverlay");
        imgGo.transform.SetParent(go.transform);
        RectTransform rt = imgGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one;
        Image img = imgGo.AddComponent<Image>(); img.color = Color.black; img.raycastTarget = true;
    }

    private void MakeBlinkCanvas()
    {
        GameObject go = new GameObject("CutsceneBlinkCanvas");
        go.transform.SetParent(transform); go.SetActive(false);
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 1000;
        blinkCanvasGroup = go.AddComponent<CanvasGroup>();
        blinkCanvasGroup.alpha = 0f; blinkCanvasGroup.blocksRaycasts = false; blinkCanvasGroup.interactable = false;
        GameObject imgGo = new GameObject("BlinkOverlay");
        imgGo.transform.SetParent(go.transform);
        RectTransform rt = imgGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one;
        Image img = imgGo.AddComponent<Image>(); img.color = Color.black; img.raycastTarget = false;
    }

    private Volume MakeVolume()
    {
        if (cutsceneVolume != null) return cutsceneVolume;
        GameObject go = new GameObject("CutsceneVolume"); go.transform.SetParent(transform);
        Volume v = go.AddComponent<Volume>(); v.isGlobal = true; v.weight = 0f; v.priority = 100f;
        cutsceneVolume = v; return v;
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        if (fadeCanvasGroup == null) yield break;
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t / dur); yield return null; }
        fadeCanvasGroup.alpha = to;
    }
}
