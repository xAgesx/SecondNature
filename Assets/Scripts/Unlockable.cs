using System.Collections;
using UnityEngine;

public class Unlockable : MonoBehaviour
{
    [Tooltip("The door panel Transform to rotate open (pivot from hinge edge).")]
    public Transform doorPivot;

    [Tooltip("Target Y rotation when the door is fully open.")]
    public float openAngle = 120f;

    [Tooltip("How long the open animation takes in seconds.")]
    public float openDuration = 0.4f;

    [Header("Voice Line")]
    public AudioClip companionClip;

    [Tooltip("Seconds to wait after the ruler is picked up before the door opens.")]
    public float openDelay = 4f;

    private bool _isOpen = false;

    // Call this from the ruler's GrabInteractable
    // First Select event in the Inspector
    public void OnRulerPickedUp()
    {
        if (_isOpen) return;
        StartCoroutine(DelayedOpen());
        Debug.Log("[Unlockable] Ruler picked up — door opens in " + openDelay + "s.");
    }

    private IEnumerator DelayedOpen()
    {
        yield return new WaitForSeconds(openDelay);
        TryOpen();
    }

    public void TryOpen()
    {
        if (_isOpen) return;
        _isOpen = true;
        StartCoroutine(OpenAndSpeak());
    }

    private IEnumerator OpenAndSpeak()
    {
        if (doorPivot == null) { Debug.LogWarning("[Unlockable] doorPivot not assigned."); yield break; }

        Quaternion startRot = doorPivot.localRotation;
        Quaternion endRot = Quaternion.Euler(0f, openAngle, 0f);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            doorPivot.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }
        doorPivot.localRotation = endRot;

        if (companionClip != null && CompanionAI.Instance != null)
            CompanionAI.Instance.PlayVoiceLine(companionClip, false);

        Debug.Log($"[Unlockable] '{gameObject.name}' opened.");
    }
}