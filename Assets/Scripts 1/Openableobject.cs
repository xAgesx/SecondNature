using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to any door or window that can actually be opened or broken.
/// 
/// IMPORTANT: Not every door/window needs this component.
/// - Has tag "Door" or "Window" + HAS this component  → can be opened with the right tool
/// - Has tag "Door" or "Window" + NO this component   → always blocked (locked/jammed door)
/// 
/// SETUP:
///   1. Tag the GameObject "Door" or "Window" (set in Inspector top bar).
///   2. Add this component.
///   3. Set isHot in the Inspector to simulate fire on the other side.
///   4. Assign the pivot Transform if you want a rotation-based open animation.
///   5. Make sure the GameObject has a Collider (can be non-trigger — the HAND
///      collider on HandInteractionBlocker handles the trigger side).
/// </summary>
public class OpenableObject : MonoBehaviour
{
    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("Fire state")]
    [Tooltip("Is there fire on the other side? Set this true in the Inspector to simulate danger. " +
             "Later you can drive this from a FireSourceManager.")]
    public bool isHot = false;

    [Header("Open animation")]
    [Tooltip("The Transform to rotate when opening. Usually the door panel itself, " +
             "pivoting around its hinge edge. Leave null to skip animation (e.g. sliding doors " +
             "you'll animate yourself).")]
    public Transform doorPivot;

    [Tooltip("How many degrees to rotate on the Y axis when opening. " +
             "Positive = rotates right (outward), negative = rotates left.")]
    public float openAngle = 90f;

    [Tooltip("How long the open animation takes in seconds.")]
    public float openDuration = 0.4f;

    [Header("Visual feedback")]
    [Tooltip("Renderer whose material tints red on bare-hand contact and orange when hot. " +
             "Assign the door panel renderer here.")]
    public Renderer doorRenderer;

    [Tooltip("How long the red/orange tint stays visible before fading back to normal.")]
    public float tintFadeDuration = 0.6f;

    // ─── Private state ────────────────────────────────────────────────────────

    private bool _isOpen = false;           // has this door/window been opened?
    private bool _isTinting = false;        // is a tint coroutine running?
    private Color _originalColor;          // cached so we can restore it
    private static readonly int ColorProp = Shader.PropertyToID("_BaseColor"); // URP property

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        if (doorRenderer != null)
            _originalColor = doorRenderer.material.GetColor(ColorProp);
    }

    // ─── Public interaction entry points ─────────────────────────────────────
    // These are called by HandInteractionBlocker (on the hands) and
    // ToolInteractionHandler (on the tools). You don't call these directly.

    /// <summary>
    /// Called when a bare hand touches this door/window.
    /// Always blocked — bare skin shouldn't touch potentially hot surfaces.
    /// </summary>
    /// <param name="handDevice">The controller to send haptics to.</param>
    /// <param name="runner">A MonoBehaviour to host the haptic coroutine.</param>
    public void OnBareHandContact(UnityEngine.XR.InputDevice handDevice, MonoBehaviour runner)
    {
        if (_isOpen) return; // already open, nothing to block

        // 1. Haptic: double buzz "no" signal
        VRHaptics.SendPattern(runner, handDevice, VRHaptics.BlockedDouble);

        // 2. Visual: flash the door red
        ShowTint(Color.red, runner);

        Debug.Log($"[OpenableObject] Bare hand blocked on '{gameObject.name}'.");
    }

    /// <summary>
    /// Called when a SchoolTool touches this door/window.
    /// </summary>
    /// <param name="tool">The tool that made contact.</param>
    /// <param name="runner">A MonoBehaviour to host coroutines.</param>
    public void OnToolContact(SchoolTool tool, MonoBehaviour runner)
    {
        if (_isOpen) return; // already open

        // ── Hot door / window check ───────────────────────────────────────────
        if (isHot)
        {
            // Tool detected heat — warn but do NOT open
            VRHaptics.SendPattern(runner, tool.holdingController, VRHaptics.HeatWarning);
            ShowTint(new Color(1f, 0.45f, 0f), runner); // orange tint = heat
            Debug.Log($"[OpenableObject] '{tool.toolName}' detected heat on '{gameObject.name}'. Not opening.");
            return;
        }

        // ── Capability check ─────────────────────────────────────────────────
        bool isDoor = gameObject.CompareTag("Door");
        bool isWindow = gameObject.CompareTag("Window");

        if (isDoor && tool.canOpenDoors)
        {
            Open(tool, runner);
        }
        else if (isWindow && tool.canBreakWindows)
        {
            Break(tool, runner);
        }
        else
        {
            // Tool isn't capable enough (e.g. ruler on window)
            VRHaptics.SendPattern(runner, tool.holdingController, VRHaptics.WeakBump);
            Debug.Log($"[OpenableObject] '{tool.toolName}' can't interact with '{gameObject.name}'.");
        }
    }

    // ─── Open / Break ─────────────────────────────────────────────────────────

    private void Open(SchoolTool tool, MonoBehaviour runner)
    {
        _isOpen = true;

        // Haptic: satisfying click
        VRHaptics.SendPattern(runner, tool.holdingController, VRHaptics.SuccessClick);

        // Animation
        if (doorPivot != null)
            runner.StartCoroutine(AnimateOpen());

        Debug.Log($"[OpenableObject] '{gameObject.name}' opened by '{tool.toolName}'.");
    }

    private void Break(SchoolTool tool, MonoBehaviour runner)
    {
        _isOpen = true;

        // Haptic: heavy thud (reuse SuccessClick — you can make a dedicated one later)
        VRHaptics.SendPattern(runner, tool.holdingController, VRHaptics.SuccessClick);

        // For now: deactivate the window GameObject (swap for shatter VFX later)
        gameObject.SetActive(false);

        Debug.Log($"[OpenableObject] '{gameObject.name}' broken by '{tool.toolName}'.");
    }

    // ─── Animation ────────────────────────────────────────────────────────────

    private IEnumerator AnimateOpen()
    {
        Quaternion startRot = doorPivot.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, openAngle, 0f);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            // Smooth step easing — starts fast, slows to a stop like a real door
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            doorPivot.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        doorPivot.localRotation = endRot; // snap to exact final rotation
    }

    // ─── Visual tint ──────────────────────────────────────────────────────────

    private void ShowTint(Color tintColor, MonoBehaviour runner)
    {
        if (doorRenderer == null) return;
        if (_isTinting) runner.StopCoroutine(nameof(FadeTint)); // cancel previous tint
        runner.StartCoroutine(FadeTint(tintColor));
    }

    private IEnumerator FadeTint(Color tintColor)
    {
        _isTinting = true;

        // Apply tint immediately
        doorRenderer.material.SetColor(ColorProp, tintColor);

        // Hold for a moment
        yield return new WaitForSeconds(0.15f);

        // Fade back to original
        float elapsed = 0f;
        while (elapsed < tintFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / tintFadeDuration;
            doorRenderer.material.SetColor(ColorProp, Color.Lerp(tintColor, _originalColor, t));
            yield return null;
        }

        doorRenderer.material.SetColor(ColorProp, _originalColor);
        _isTinting = false;
    }
}