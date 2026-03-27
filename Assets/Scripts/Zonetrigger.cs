using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ZoneTrigger
// Attach to: Smoke VFX GameObject + Fire VFX GameObject
//
// Setup:
//   1. Add a Box/Sphere Collider to your smoke or fire VFX object.
//   2. Set Is Trigger = ON.
//   3. Add this script and set the zoneType to match.
//
// Detects the player entering/exiting and calls the right method
// on PlayerDamageHandler.
// ─────────────────────────────────────────────────────────────────────────────
public class ZoneTrigger : MonoBehaviour
{
    public enum ZoneType { Smoke, Fire }

    [Tooltip("What kind of zone is this?")]
    public ZoneType zoneType = ZoneType.Smoke;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var handler = other.GetComponent<PlayerDamageHandler>();
        if (handler == null) return;

        if (zoneType == ZoneType.Smoke) handler.OnEnterSmokeZone();
        if (zoneType == ZoneType.Fire) handler.OnEnterFireZone();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var handler = other.GetComponent<PlayerDamageHandler>();
        if (handler == null) return;

        if (zoneType == ZoneType.Smoke) handler.OnExitSmokeZone();
        if (zoneType == ZoneType.Fire) handler.OnExitFireZone();
    }
}