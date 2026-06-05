using UnityEngine;

public class ProximityDetector : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Drag your XR Origin (or Camera Offset) here — no tag needed.")]
    public Transform playerTransform;

    [Header("Detection")]
    public float detectionRadius = 1.5f;
    public LayerMask detectionLayers = ~0;

    [Header("Debug")]
    public bool showGizmo = true;

    private bool _wasInFire = false;
    private bool _wasInSmoke = false;
    private PlayerDamageHandler _handler;

    private void Awake()
    {
        // Try to get PlayerDamageHandler from the assigned player transform first,
        // then fall back to this GameObject
        if (playerTransform != null)
            _handler = playerTransform.GetComponentInChildren<PlayerDamageHandler>();

        if (_handler == null)
            _handler = GetComponent<PlayerDamageHandler>();

        if (_handler == null)
            Debug.LogError("[ProximityDetector] No PlayerDamageHandler found. " +
                           "Attach PlayerDamageHandler to the same GameObject or assign playerTransform.");
    }

    private void FixedUpdate()
    {
        if (_handler == null) return;

        // Use assigned player position if available, otherwise use this transform
        Vector3 origin = playerTransform != null ? playerTransform.position : transform.position;

        bool inFire = false;
        bool inSmoke = false;

        Collider[] hits = Physics.OverlapSphere(origin, detectionRadius, detectionLayers);
        foreach (var col in hits)
        {
            if (col.CompareTag("Fire")) inFire = true;
            if (col.CompareTag("Smoke")) inSmoke = true;
            if (inFire && inSmoke) break;
        }

        if (inFire && !_wasInFire) _handler.OnEnterFireZone();
        else if (!inFire && _wasInFire) _handler.OnExitFireZone();

        if (inSmoke && !_wasInSmoke) _handler.OnEnterSmokeZone();
        else if (!inSmoke && _wasInSmoke) _handler.OnExitSmokeZone();

        _wasInFire = inFire;
        _wasInSmoke = inSmoke;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Vector3 origin = playerTransform != null ? playerTransform.position : transform.position;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawSphere(origin, detectionRadius);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
        Gizmos.DrawWireSphere(origin, detectionRadius);
    }
#endif
}