using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class SmokeViewPostFX : MonoBehaviour {
    [Header("Player Filter")]
    [Tooltip("Tag the incoming collider must have. Leave empty to accept any collider.")]
    public string playerTag = "Player";

    [Header("Fade Timing")]
    [Range(0f, 1f)]
    [Tooltip("Peak Volume weight while the player is inside the zone.")]
    public float peakWeight = 1f;
    [Tooltip("Seconds to fade IN to peak when the player enters.")]
    public float fadeInDuration = 0.6f;
    [Tooltip("Seconds to fade OUT to zero when the player leaves.")]
    public float fadeOutDuration = 1.2f;

    private Volume _volume;
    private Coroutine _fadeCoroutine;
    private int _insideCount;

    private void Awake() {
        _volume = GetComponent<Volume>();
        _volume.isGlobal = true;
        _volume.weight = 0f;
    }

    private void OnTriggerEnter(Collider other) {
        Debug.Log(other.name);
        // if (!IsPlayer(other)) return;
        _insideCount++;
        if (_insideCount == 1) StartFade(peakWeight, fadeInDuration);
    }

    private void OnTriggerExit(Collider other) {
        // if (!IsPlayer(other)) return;
        _insideCount = Mathf.Max(0, _insideCount - 1);
        if (_insideCount == 0) StartFade(0f, fadeOutDuration);
    }

    private bool IsPlayer(Collider other) {
        if (other == null) return false;
        if (string.IsNullOrEmpty(playerTag)) return true;
        return other.CompareTag(playerTag);
    }

    private void StartFade(float targetWeight, float duration) {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(targetWeight, duration));
    }

    private IEnumerator FadeRoutine(float targetWeight, float duration) {
        float startWeight = _volume.weight;
        float t = 0f;
        float d = Mathf.Max(0.0001f, duration);

        while (t < d) {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / d);
            _volume.weight = Mathf.SmoothStep(startWeight, targetWeight, n);
            yield return null;
        }

        _volume.weight = targetWeight;
        _fadeCoroutine = null;
    }

    private void OnDisable() {
        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
        if (_volume != null) _volume.weight = 0f;
        _insideCount = 0;
    }
}
