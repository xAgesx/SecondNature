using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach this to any tool the player can pick up (ruler, textbook, pencil, etc.)
/// It declares what the tool is capable of doing to doors and windows.
/// 
/// SETUP:
///   1. Add this component to your tool prefab (e.g. HallPassRuler).
///   2. Tick the capabilities that fit the tool (see Inspector tooltips below).
///   3. Make sure the prefab also has an XRGrabInteractable so the player can pick it up.
///   4. Make sure it has a Collider marked as Trigger (IsTrigger = true).
/// </summary>
public class SchoolTool : MonoBehaviour
{
    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("Tool identity")]
    [Tooltip("Friendly name shown in debug logs. E.g. 'Hall Pass Ruler'.")]
    public string toolName = "School Tool";

    [Header("Capabilities")]
    [Tooltip("Can this tool press/hook a door handle to open a door? (Ruler, pencil, backpack strap)")]
    public bool canOpenDoors = true;

    [Tooltip("Can this tool smash a window? Requires mass/force. (Textbook, fire extinguisher)")]
    public bool canBreakWindows = false;

    // ─── Runtime state ────────────────────────────────────────────────────────

    // The XR controller this tool is currently held in.
    // Filled automatically when the player grabs/releases the tool.
    [HideInInspector] public InputDevice holdingController;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Listen to XRGrabInteractable events to track which hand is holding the tool
        var grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
        {
            Debug.LogWarning($"[SchoolTool] '{toolName}' has no XRGrabInteractable. " +
                              "Haptics won't know which controller to vibrate.");
            return;
        }

        // selectEntered fires when the player grabs the object
        grab.selectEntered.AddListener(OnGrabbed);
        // selectExited fires when the player drops the object
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        var grab = GetComponent<XRGrabInteractable>();
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    // ─── Grab / release tracking ──────────────────────────────────────────────

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // XRI 3.x removed XRBaseControllerInteractor and xrController.inputDevice.
        // Instead we ask the interactor which XRNode (hand) it represents,
        // then look up the live InputDevice for that node ourselves.

        // Step 1: get the Transform of the hand that grabbed us
        var interactorTransform = args.interactorObject.transform;

        // Step 2: determine handedness by checking the XRNode on the nearest
        // TrackedPoseDriver, or fall back to name-based heuristic
        XRNode node = ResolveHandNode(interactorTransform);

        // Step 3: find the live InputDevice at that node
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);
        foreach (var d in devices)
        {
            if (d.isValid)
            {
                holdingController = d;
                return;
            }
        }

        Debug.LogWarning($"[SchoolTool] Could not resolve InputDevice for node {node}.");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Reset — tool is in the air, no controller to vibrate
        holdingController = default;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Figures out whether the grabbing hand is Left or Right.
    /// 
    /// XRI 3.x stores handedness directly on the interactor as a
    /// UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor
    /// (via its IXRInteractor.handedness property on some builds).
    /// The most robust cross-version approach is a name check fallback:
    /// hand GameObjects in the XR Origin prefab are conventionally named
    /// "Left Hand" / "Right Hand" or "LeftHand" / "RightHand".
    /// </summary>
    private XRNode ResolveHandNode(Transform interactorTransform)
    {
        // Walk up the hierarchy looking for a name hint
        Transform t = interactorTransform;
        while (t != null)
        {
            string nameLower = t.name.ToLower();
            if (nameLower.Contains("left")) return XRNode.LeftHand;
            if (nameLower.Contains("right")) return XRNode.RightHand;
            t = t.parent;
        }

        // Default to right hand if we genuinely can't tell
        Debug.LogWarning("[SchoolTool] Could not determine handedness from hierarchy name. " +
                         "Defaulting to RightHand. Rename your hand GameObjects to include " +
                         "'Left' or 'Right' to fix this.");
        return XRNode.RightHand;
    }
}