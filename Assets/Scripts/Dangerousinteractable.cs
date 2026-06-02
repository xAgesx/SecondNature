using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ─────────────────────────────────────────────────────────────────────────────
// DangerousInteractable
// Attach to: Any hot Door or Window GameObject
//
// Requires an XRBaseInteractable (XRGrabInteractable or XRSimpleInteractable)
// on the same object. Fires damage via PlayerDamageHandler when a BARE hand
// grabs the object (selectEntered — grip press only, not hover or ray).
//
// Player detection strategy (most-reliable-first):
//   1. Walk up from the interactor transform → finds PlayerDamageHandler on
//      the XR Rig root even when the collider lives on a child object.
//   2. Check the interactor's GameObject tag == "Player" as a sanity gate.
//   3. Fall back to FindObjectOfType as a last resort (editor warning issued).
//
// Setup:
//   1. Add XRGrabInteractable or XRSimpleInteractable to your Door/Window.
//   2. Tag the XR Rig root (or its direct hands) with the "Player" tag.
//   3. Attach this script to the same Door/Window GameObject.
// ─────────────────────────────────────────────────────────────────────────────
public class DangerousInteractable : MonoBehaviour
{
    [Tooltip("Seconds between repeated damage hits while the player holds the object.")]
    public float damageCooldown = 1f;

    [Tooltip("Only trigger damage when the interactor has this tag. " +
             "Set to 'Player' (default). Leave blank to allow any interactor.")]
    public string requiredInteractorTag = "Player";

    private XRBaseInteractable _interactable;
    private float _lastDamageTime = -999f;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();

        if (_interactable == null)
        {
            Debug.LogError($"[DangerousInteractable] '{gameObject.name}' — no XRBaseInteractable found. " +
                           "Add XRGrabInteractable or XRSimpleInteractable to this object.");
            return;
        }

        _interactable.selectEntered.AddListener(OnPlayerGrab);
        Debug.Log($"[DangerousInteractable] '{gameObject.name}' ready — damage fires on grip.");
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(OnPlayerGrab);
    }

    // ── Grab callback ─────────────────────────────────────────────────────────

    private void OnPlayerGrab(SelectEnterEventArgs args)
    {
        // ── Cooldown ──────────────────────────────────────────────────────────
        if (Time.time - _lastDamageTime < damageCooldown) return;

        Transform interactorTransform = args.interactorObject.transform;

        // ── Tag gate (optional) ───────────────────────────────────────────────
        // Walks up the hierarchy to see if any ancestor carries the Player tag.
        // This handles setups where the interactor lives on a child of the rig.
        if (!string.IsNullOrEmpty(requiredInteractorTag))
        {
            bool tagFound = false;
            Transform t = interactorTransform;
            while (t != null)
            {
                if (t.CompareTag(requiredInteractorTag)) { tagFound = true; break; }
                t = t.parent;
            }

            if (!tagFound)
            {
                // Not the player — could be an AI hand or physics object; ignore silently.
                return;
            }
        }

        // ── Find PlayerDamageHandler ──────────────────────────────────────────
        // Strategy 1: walk up from the interactor (most reliable)
        var handler = interactorTransform.GetComponentInParent<PlayerDamageHandler>();

        // Strategy 2: walk up from the interactor's root (handles deep rig hierarchies)
        if (handler == null)
            handler = interactorTransform.root.GetComponentInChildren<PlayerDamageHandler>();

        // Strategy 3: scene-wide fallback
        if (handler == null)
        {
            handler = FindObjectOfType<PlayerDamageHandler>();

            if (handler != null)
                Debug.LogWarning($"[DangerousInteractable] '{gameObject.name}' — used scene-wide " +
                                 "FindObjectOfType to locate PlayerDamageHandler. " +
                                 "For best performance, ensure it sits on the XR Rig root.");
        }

        if (handler == null)
        {
            Debug.LogWarning($"[DangerousInteractable] '{gameObject.name}' — grabbed but " +
                             "PlayerDamageHandler not found anywhere. Check your XR Rig setup.");
            return;
        }

        _lastDamageTime = Time.time;
        handler.OnBareHandTouchedSurface(gameObject.name);
    }
}