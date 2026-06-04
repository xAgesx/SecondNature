using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ─────────────────────────────────────────────────────────────────────────────
// CompanionVoiceTrigger
// Attach to: Any area GameObject OR any XRI interactable object
//
// Two trigger modes — both can be active at the same time:
//
//   AREA MODE  — add a Collider (Is Trigger ticked) to this object.
//                Voice line fires when the player walks in.
//
//   GRAB MODE  — add an XRGrabInteractable or XRSimpleInteractable.
//                Voice line fires when the player grabs the object.
//
// ─────────────────────────────────────────────────────────────────────────────
public class CompanionVoiceTrigger : MonoBehaviour
{
    [Header("Voice Line")]
    [Tooltip("The audio clip the companion plays when this trigger fires.")]
    public AudioClip voiceLine;

    [Tooltip("Optional label shown in debug logs to identify this trigger.")]
    public string triggerName = "Trigger";

    [Header("Trigger Settings")]
    [Tooltip("If ticked, the companion will play the line every time — not just once.")]
    public bool allowRepeat = false;

    [Tooltip("Delay in seconds before the voice line plays after the trigger fires.")]
    public float speakDelay = 0.5f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool _triggered = false;
    private XRBaseInteractable _interactable;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Hook into XRI grab if an interactable exists on this object
        _interactable = GetComponent<XRBaseInteractable>();
        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnGrabbed);
            Debug.Log($"[CompanionVoiceTrigger] '{triggerName}' — grab trigger registered.");
        }
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(OnGrabbed);
    }

    // ── Area trigger (player walks in) ────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        TryFire("area enter");
    }

    // ── Grab trigger ──────────────────────────────────────────────────────────

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        TryFire("grab");
    }

    // ── Shared fire logic ─────────────────────────────────────────────────────

    private void TryFire(string source)
    {
        if (_triggered && !allowRepeat) return;

        if (voiceLine == null)
        {
            Debug.LogWarning($"[CompanionVoiceTrigger] '{triggerName}' fired by {source} but no voice line assigned.");
            return;
        }

        if (CompanionAI.Instance == null)
        {
            Debug.LogWarning($"[CompanionVoiceTrigger] '{triggerName}' — no CompanionAI instance found in scene.");
            return;
        }

        if (!allowRepeat && CompanionAI.Instance.HasPlayed(voiceLine)) return;

        _triggered = true;

        Debug.Log($"[CompanionVoiceTrigger] '{triggerName}' triggered by {source} — playing '{voiceLine.name}'.");

        if (speakDelay > 0f)
            Invoke(nameof(Speak), speakDelay);
        else
            Speak();
    }

    private void Speak()
    {
        CompanionAI.Instance?.PlayVoiceLine(voiceLine, allowRepeat);
    }
}