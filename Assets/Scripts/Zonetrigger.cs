using UnityEngine;

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