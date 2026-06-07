using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class FireHeatHazePostFX : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Drag your XR Origin (or Camera Offset) here — no tag needed.")]
    public Transform playerTransform;

    [Header("Detection (mirrors ProximityDetector)")]
    public float detectionRadius = 1.5f;
    public LayerMask detectionLayers = ~0;
    public string fireTag = "Fire";

    [Header("Burn Pulse")]
    [Tooltip("Total duration of the post-FX pulse in seconds.")]
    public float duration = 3f;
    [Range(0f, 1f)]
    [Tooltip("Peak Volume weight during the pulse (1 = full profile influence).")]
    public float peakWeight = 1f;
    [Range(0f, 0.95f)]
    [Tooltip("Fraction of duration spent easing IN to peak.")]
    public float easeInFraction = 0.1f;
    [Range(0f, 0.95f)]
    [Tooltip("Fraction of duration spent easing OUT from peak (long fade-out).")]
    public float easeOutFraction = 0.9f;
    [Tooltip("Optional custom curve mapping normalized time (0..1) to weight (0..1). Overrides ease-in/out if it has more than one keyframe.")]
    public AnimationCurve weightCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 10f),
        new Keyframe(0.1f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, -1.1f, 0f));

    [Header("Debug")]
    public bool showGizmo = false;
    public bool retriggerOnReentryOnly = true;

    private Volume _volume;
    private Coroutine _pulseCoroutine;
    private bool _wasInFire;

    private void Awake()
    {
        _volume = GetComponent<Volume>();
        _volume.isGlobal = true;
        _volume.weight = 0f;
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        Vector3 origin = playerTransform.position;
        Collider[] hits = Physics.OverlapSphere(origin, detectionRadius, detectionLayers);

        bool inFire = false;
        foreach (var col in hits)
        {
            if (col != null && col.CompareTag(fireTag)) { inFire = true; break; }
        }

        if (inFire && (!_wasInFire || !retriggerOnReentryOnly))
            TriggerBurn();

        _wasInFire = inFire;
    }

    public void TriggerBurn()
    {
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float t = 0f;
        bool useCurve = weightCurve != null && weightCurve.length > 1;
        float inT = Mathf.Clamp(easeInFraction, 0f, 0.95f);
        float outT = Mathf.Clamp(easeOutFraction, 0f, 0.95f);
        if (inT + outT > 1f)
        {
            float scale = 1f / (inT + outT);
            inT *= scale;
            outT *= scale;
        }
        float holdEnd = 1f - outT;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));

            float w;
            if (useCurve)
            {
                w = weightCurve.Evaluate(n) * peakWeight;
            }
            else
            {
                if (n < inT)
                    w = Mathf.SmoothStep(0f, peakWeight, n / Mathf.Max(0.0001f, inT));
                else if (n < holdEnd)
                    w = peakWeight;
                else
                    w = Mathf.SmoothStep(peakWeight, 0f, (n - holdEnd) / Mathf.Max(0.0001f, outT));
            }

            _volume.weight = w;
            yield return null;
        }

        _volume.weight = 0f;
        _pulseCoroutine = null;
    }

    private void OnDisable()
    {
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        if (_volume != null) _volume.weight = 0f;
        _wasInFire = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Vector3 origin = playerTransform != null ? playerTransform.position : transform.position;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawSphere(origin, detectionRadius);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(origin, detectionRadius);
    }
#endif
}
