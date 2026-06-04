using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// CompanionVoiceLine
// Attach to: Any GameObject that should trigger a companion voice line
//            (doors, windows, UI panels, fire extinguishers, etc.)
//
// This is a thin hook. It doesn't decide *when* to speak — the script that
// knows the right moment calls Trigger() on this component.
//
// How each type of trigger calls it:
//
//   ► GRAB / INTERACT (XRI):
//       In DangerousInteractable or your own XRI script, get this component
//       and call:  GetComponent<CompanionVoiceLine>()?.Trigger();
//
//   ► UI CHOICE:
//       In your UI button handler call:
//       yourObject.GetComponent<CompanionVoiceLine>()?.Trigger();
//       — or assign the CompanionVoiceLine reference directly and call Trigger().
//
//   ► OTHER SCRIPTS:
//       Cache a reference to this component and call Trigger() at the right time.
//
//   ► UNITY EVENTS / INSPECTOR:
//       Wire Trigger() directly to a UnityEvent in the Inspector — no code needed.
//
// Setup:
//   1. Attach this script to the relevant object.
//   2. Assign the voiceLine AudioClip in the Inspector.
//   3. Call Trigger() from whatever script/event handles the interaction.
// ─────────────────────────────────────────────────────────────────────────────
public class CompanionVoiceLine : MonoBehaviour
{
    [Header("Voice Line")]
    [Tooltip("The clip the companion plays when this object's event fires.")]
    public AudioClip voiceLine;

    [Tooltip("Optional label for debug logs.")]
    public string lineName = "Voice Line";

    [Tooltip("Delay in seconds before the companion speaks after Trigger() is called.")]
    public float speakDelay = 0.3f;

    [Tooltip("If ticked, the line can play every time Trigger() is called. " +
             "Otherwise once per session only.")]
    public bool allowRepeat = false;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from any script or UnityEvent to make the companion speak.
    /// Respects the once-per-session rule unless allowRepeat is ticked.
    /// </summary>
    public void Trigger()
    {
        if (voiceLine == null)
        {
            Debug.LogWarning($"[CompanionVoiceLine] '{lineName}' on '{gameObject.name}' — Trigger() called but no clip assigned.");
            return;
        }

        if (CompanionAI.Instance == null)
        {
            Debug.LogWarning($"[CompanionVoiceLine] '{lineName}' — no CompanionAI instance in scene.");
            return;
        }

        if (!allowRepeat && CompanionAI.Instance.HasPlayed(voiceLine))
        {
            Debug.Log($"[CompanionVoiceLine] '{lineName}' already played this session — skipping.");
            return;
        }

        if (speakDelay > 0f)
            Invoke(nameof(Speak), speakDelay);
        else
            Speak();

        Debug.Log($"[CompanionVoiceLine] Triggering '{lineName}' from '{gameObject.name}'.");
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void Speak()
    {
        CompanionAI.Instance?.PlayVoiceLine(voiceLine, allowRepeat);
    }
}