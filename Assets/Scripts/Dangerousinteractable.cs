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

        // selectEntered fires when the player squeezes/grips the object.
        // It does NOT fire on hover or ray pointer proximity.
        _interactable.selectEntered.AddListener(OnPlayerGrab);

        Debug.Log($"[DangerousInteractable] '{gameObject.name}' armed — damage triggers on grip/select only ✔");
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
        {
            Debug.Log($"[DangerousInteractable] '{gameObject.name}' grabbed but on cooldown " +
                      $"({(damageCooldown - (Time.time - _lastDamageTime)):F1}s remaining).");
            return;
        }

        _lastDamageTime = Time.time;

        // Walk up from the interactor to find PlayerDamageHandler on the XR Rig root
        var handler = args.interactorObject.transform.GetComponentInParent<PlayerDamageHandler>();

        if (handler == null)
        {
            Debug.LogWarning($"[DangerousInteractable] '{gameObject.name}' was grabbed but no PlayerDamageHandler " +
                             "found in the interactor hierarchy. Is PlayerDamageHandler on the XR Rig root?");
            return;
        }

        float tempBefore = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;

        Debug.Log($"[DangerousInteractable] 🖐️ '{gameObject.name}' grabbed bare-handed | " +
                  $"Temp before: {tempBefore:F1}°C — applying damage...");

        handler.OnBareHandTouchedSurface();

        float tempAfter = TemperatureSystem.Instance != null ? TemperatureSystem.Instance.CurrentTemp : 0f;
        Debug.Log($"[DangerousInteractable] ✔ Damage applied | " +
                  $"Temp: {tempBefore:F1}°C → {tempAfter:F1}°C (+{tempAfter - tempBefore:F1}°C)");
    }
}