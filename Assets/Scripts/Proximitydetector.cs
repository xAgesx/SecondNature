using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ProximityDetector
// Attach to: XR Rig root (same object as PlayerDamageHandler)
//
// Replaces ZoneTrigger. No trigger colliders needed on hazard objects.
// Every FixedUpdate it runs an OverlapSphere from the player's position and
// checks for GameObjects tagged "Fire" or "Smoke" within range.
//
// Setup:
//   1. Tag your fire hazard GameObjects with the tag "Fire".
//   2. Tag your smoke hazard GameObjects with the tag "Smoke".
//   3. Attach this script to the XR Rig root alongside PlayerDamageHandler.
//   4. Tune detectionRadius in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────
public class ProximityDetector : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Radius in metres around the player to scan for Fire/Smoke objects.")]
    public float detectionRadius = 1.5f;

    [Tooltip("Layer mask to limit the overlap sphere (optional — leave 'Everything' to scan all layers).")]
    public LayerMask detectionLayers = ~0;

    [Header("Debug")]
    [Tooltip("Draw the detection sphere in the Scene view (editor only).")]
    public bool showGizmo = true;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private bool _wasInFire = false;
    private bool _wasInSmoke = false;

    private PlayerDamageHandler _handler;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        _handler = GetComponent<PlayerDamageHandler>();
        if (_handler == null)
            Debug.LogError("[ProximityDetector] No PlayerDamageHandler found on this GameObject. " +
                           "Attach both scripts to the XR Rig root.");
    }

    private void FixedUpdate()
    {
        if (_handler == null) return;

        bool inFire = false;
        bool inSmoke = false;

        // Single overlap call — we check tags rather than using separate spheres
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayers);

        foreach (var col in hits)
        {
            if (col.CompareTag("Fire")) inFire = true;
            if (col.CompareTag("Smoke")) inSmoke = true;
            if (inFire && inSmoke) break; // found both — no need to keep scanning
        }

        // ── Fire state transitions ─────────────────────────────────────────
        if (inFire && !_wasInFire)
            _handler.OnEnterFireZone();
        else if (!inFire && _wasInFire)
            _handler.OnExitFireZone();

        // ── Smoke state transitions ────────────────────────────────────────
        if (inSmoke && !_wasInSmoke)
            _handler.OnEnterSmokeZone();
        else if (!inSmoke && _wasInSmoke)
            _handler.OnExitSmokeZone();

        _wasInFire = inFire;
        _wasInSmoke = inSmoke;
    }

    // ── Editor helpers ────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}