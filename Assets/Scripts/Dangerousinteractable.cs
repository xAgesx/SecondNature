using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DangerousInteractable : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Drag your XR Origin root here — no tag needed.")]
    public Transform playerTransform;

    [Tooltip("Seconds between repeated damage hits while holding the object.")]
    public float damageCooldown = 1f;

    private XRBaseInteractable _interactable;
    private float _lastDamageTime = -999f;
    private PlayerDamageHandler _handler;

    private void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();
        if (_interactable == null)
        {
            Debug.LogError($"[DangerousInteractable] '{gameObject.name}' — no XRBaseInteractable found.");
            return;
        }
        _interactable.selectEntered.AddListener(OnPlayerGrab);

        // Cache handler from assigned player transform
        if (playerTransform != null)
            _handler = playerTransform.GetComponentInChildren<PlayerDamageHandler>();

        if (_handler == null)
            _handler = FindObjectOfType<PlayerDamageHandler>();

        if (_handler == null)
            Debug.LogWarning($"[DangerousInteractable] '{gameObject.name}' — PlayerDamageHandler not found. " +
                             "Assign the Player Transform in the Inspector.");
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(OnPlayerGrab);
    }

    private void OnPlayerGrab(SelectEnterEventArgs args)
    {
        if (Time.time - _lastDamageTime < damageCooldown) return;
        if (_handler == null) return;

        _lastDamageTime = Time.time;
        _handler.OnBareHandTouchedSurface(gameObject.name);
    }
}