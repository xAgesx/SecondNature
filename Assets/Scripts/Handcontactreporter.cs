using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// HandContactReporter
// Attach to: Left Hand + Right Hand GameObjects
//
// Automatically creates its own Sphere trigger collider at runtime —
// no need to manually add one to VR hands.
// ─────────────────────────────────────────────────────────────────────────────
public class HandContactReporter : MonoBehaviour
{
    [Tooltip("Radius of the hand touch sphere in metres.")]
    public float touchRadius = 0.06f;

    [Tooltip("Offset from the hand pivot to the palm centre.")]
    public Vector3 touchOffset = new Vector3(0f, -0.02f, 0.05f);

    private void Awake()
    {
        // Create a child GameObject that owns the trigger collider
        // so it never conflicts with any physics the hand rig already has
        var touchPoint = new GameObject("HandTouchPoint");
        touchPoint.transform.SetParent(transform, false);
        touchPoint.transform.localPosition = touchOffset;

        var col = touchPoint.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = touchRadius;

        // Move the reporter logic to the child so OnTriggerEnter fires there
        var reporter = touchPoint.AddComponent<HandTouchPoint>();
        reporter.owner = this;
    }

    // Called by HandTouchPoint when contact is detected
    public void ReportContact()
    {
        if (GetComponentInChildren<SchoolTool>() != null) return;

        var handler = GetComponentInParent<PlayerDamageHandler>();
        if (handler != null)
            handler.OnBareHandTouchedSurface();
    }
}