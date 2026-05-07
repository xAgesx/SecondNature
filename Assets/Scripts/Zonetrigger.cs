using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ZoneTrigger
// Attach to: Smoke VFX GameObject + Fire VFX GameObject
//
// Setup:
//   1. Add a Box/Sphere Collider to your smoke or fire VFX object.
//   2. Set Is Trigger = ON.
//   3. Add this script and set the zoneType to match.
//   4. Make sure your XR Rig root (or its CharacterController child) is tagged "Player".
//
// FIX: Uses GetComponentInParent so PlayerDamageHandler is found on the XR Rig
//      root even when the collider that enters the trigger is a child object.
// ─────────────────────────────────────────────────────────────────────────────
public class ZoneTrigger : MonoBehaviour
{
    public enum ZoneType { Smoke, Fire }

    [Tooltip("What kind of zone is this?")]
    public ZoneType zoneType = ZoneType.Smoke;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // FIX: GetComponentInParent instead of GetComponent —
        // PlayerDamageHandler lives on the XR Rig root, not on the collider itself.
        var handler = other.GetComponentInParent<PlayerDamageHandler>();

        if (handler == null)
        {
            Debug.LogWarning($"[ZoneTrigger] '{gameObject.name}' — Player entered but no " +
                             "PlayerDamageHandler found in parent hierarchy. " +
                             "Is it on the XR Rig root?");
            return;
        }

        if (zoneType == ZoneType.Smoke) handler.OnEnterSmokeZone();
        if (zoneType == ZoneType.Fire) handler.OnEnterFireZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var handler = other.GetComponentInParent<PlayerDamageHandler>();
        if (handler == null) return;

        if (zoneType == ZoneType.Smoke) handler.OnExitSmokeZone();
        if (zoneType == ZoneType.Fire) handler.OnExitFireZone();
    }
}using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ZoneTrigger
// Attach to: Smoke VFX GameObject + Fire VFX GameObject
//
// Setup:
//   1. Add a Box/Sphere Collider to your smoke or fire VFX object.
//   2. Set Is Trigger = ON.
//   3. Add this script and set the zoneType to match.
//   4. Make sure your XR Rig root (or its CharacterController child) is tagged "Player".
//
// FIX: Uses GetComponentInParent so PlayerDamageHandler is found on the XR Rig
//      root even when the collider that enters the trigger is a child object.
// ─────────────────────────────────────────────────────────────────────────────
public class ZoneTrigger : MonoBehaviour
{
    public enum ZoneType { Smoke, Fire }

    [Tooltip("What kind of zone is this?")]
    public ZoneType zoneType = ZoneType.Smoke;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // FIX: GetComponentInParent instead of GetComponent —
        // PlayerDamageHandler lives on the XR Rig root, not on the collider itself.
        var handler = other.GetComponentInParent<PlayerDamageHandler>();

        if (handler == null)
        {
            Debug.LogWarning($"[ZoneTrigger] '{gameObject.name}' — Player entered but no " +
                             "PlayerDamageHandler found in parent hierarchy. " +
                             "Is it on the XR Rig root?");
            return;
        }

        if (zoneType == ZoneType.Smoke) handler.OnEnterSmokeZone();
        if (zoneType == ZoneType.Fire) handler.OnEnterFireZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var handler = other.GetComponentInParent<PlayerDamageHandler>();
        if (handler == null) return;

        if (zoneType == ZoneType.Smoke) handler.OnExitSmokeZone();
        if (zoneType == ZoneType.Fire) handler.OnExitFireZone();
    }
}