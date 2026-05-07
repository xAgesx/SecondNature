using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ─────────────────────────────────────────────────────────────────────────────
// DangerousInteractable
// Attach to: Any Door or Window GameObject
//
// Requires an XRBaseInteractable (XRGrabInteractable or XRSimpleInteractable).
// Uses selectEntered — fires ONLY on grip/squeeze, NOT on hover or ray point.
//
// Setup:
//   1. Add XRGrabInteractable or XRSimpleInteractable to your Door/Window.
//   2. Add this script to the same object.
// ─────────────────────────────────────────────────────────────────────────────
public class DangerousInteractable : MonoBehaviour
{
    [Tooltip("Cooldown in seconds between repeated damage hits. Prevents spam if the player holds the grip.")]
    public float damageCooldown = 1f;

    private XRBaseInteractable _interactable;
    private float _lastDamageTime = -999f;

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();

        if (_interactable == null)
        {
            Debug.LogError($"[DangerousInteractable] '{gameObject.name}' has no XRBaseInteractable. " +
                           "Add an XRGrabInteractable or XRSimpleInteractable — damage won't fire without it.");
            return;
        }

        _interactable.selectEntered.AddListener(OnPlayerGrab);
        Debug.Log($"[DangerousInteractable] '{gameObject.name}' is ready — will deal damage on grip.");
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(OnPlayerGrab);
    }

    private void OnPlayerGrab(SelectEnterEventArgs args)
    {
        // Cooldown check — prevents repeated hits while holding
        if (Time.time - _lastDamageTime < damageCooldown)
            return;

        _lastDamageTime = Time.time;

        // Primary: walk up from the interactor to find PlayerDamageHandler on the XR Rig root.
        var handler = args.interactorObject.transform.GetComponentInParent<PlayerDamageHandler>();

        // Fallback: scene-wide search in case the rig hierarchy is non-standard.
        if (handler == null)
        {
            handler = FindObjectOfType<PlayerDamageHandler>();

            if (handler == null)
            {
                Debug.LogWarning($"[DangerousInteractable] '{gameObject.name}' was grabbed but " +
                                 "PlayerDamageHandler couldn't be found anywhere in the scene. " +
                                 "Make sure it's on your XR Rig root.");
                return;
            }
        }

        handler.OnBareHandTouchedSurface(gameObject.name);
    }
}