using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public class AutoHoverHighlight : MonoBehaviour {
    public Material hoverMaterial;

    private Renderer objectRenderer;
    private Material originalMaterial;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

    void Awake() {
        objectRenderer = GetComponent<Renderer>();
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();

        if (objectRenderer == null) {
            Debug.LogError("AutoHoverHighlight requires a Renderer component on the same GameObject.", this);
            return;
        }

        if (hoverMaterial == null) {
            Debug.LogError("Hover Material is not assigned! Please assign it in the Inspector.", this);
            return;
        }
        originalMaterial = objectRenderer.sharedMaterial;
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);

    }

    private void OnDestroy() {
        if (interactable != null) {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
        }
    }
    private void OnHoverEntered(HoverEnterEventArgs args) {
        objectRenderer.sharedMaterial = hoverMaterial;
    }

    private void OnHoverExited(HoverExitEventArgs args) {
        if(interactable.isSelected)
        return;
        objectRenderer.sharedMaterial = originalMaterial;

    }
    private void OnSelectEntered(SelectEnterEventArgs args) {
        objectRenderer.sharedMaterial = hoverMaterial;

    }

    private void OnSelectExited(SelectExitEventArgs args) {

        objectRenderer.sharedMaterial = originalMaterial;
    }
}