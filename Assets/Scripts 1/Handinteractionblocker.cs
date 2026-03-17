using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach this to each VR hand GameObject (left and right separately).
/// It detects when the bare hand (no tool equipped) touches a tagged Door or Window,
/// then tells the OpenableObject to fire the blocked response.
/// 
/// SETUP:
///   1. Find your Left Hand and Right Hand GameObjects under the XR Rig.
///   2. Add this component to each.
///   3. Set the 'handedness' field (Left or Right).
///   4. Make sure each hand has a Collider with IsTrigger = true.
///   5. Assign the XRDirectInteractor on the same hand to 'handInteractor'.
/// 
/// HOW BARE HAND IS DETECTED:
///   The hand is considered "bare" if the XRDirectInteractor is not currently
///   selecting anything (i.e. the player isn't holding a tool).
///   If the player IS holding a tool, this script does nothing —
///   ToolCollisionForwarder on the tool handles that case instead.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HandInteractionBlocker : MonoBehaviour
{
    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Tooltip("Is this the Left or Right hand? Used to get the correct controller device.")]
    public XRNode handedness = XRNode.RightHand;

    [Tooltip("The XRDirectInteractor on this hand. Used to check if the hand is holding a tool.")]
    public XRDirectInteractor handInteractor;

    // ─── Private state ────────────────────────────────────────────────────────

    // Cached InputDevice for haptics — resolved once and reused
    private InputDevice _controller;
    private bool _deviceResolved = false;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Try to resolve the device on enable; retry in Update if not found yet
        TryResolveDevice();
    }

    private void Update()
    {
        // Controllers can connect after scene start — keep retrying until found
        if (!_deviceResolved)
            TryResolveDevice();
    }

    // ─── Trigger detection ────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // Only care about objects tagged Door or Window
        if (!other.CompareTag("Door") && !other.CompareTag("Window"))
            return;

        // If the hand is currently holding a tool, do nothing here.
        // The tool's own ToolCollisionForwarder will handle the interaction.
        if (IsHoldingTool())
            return;

        // Bare hand contact — find the OpenableObject (if any) and notify it
        var openable = other.GetComponent<OpenableObject>();
        if (openable == null)
        {
            // No OpenableObject → this door/window is just decorative or permanently locked.
            // Still give the blocked haptic so the player knows they can't open it bare-handed.
            SendBlockedHaptic();
            return;
        }

        openable.OnBareHandContact(_controller, this);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private bool IsHoldingTool()
    {
        if (handInteractor == null) return false;
        // hasSelection is true when the interactor is gripping something
        return handInteractor.hasSelection;
    }

    private void SendBlockedHaptic()
    {
        // Standalone haptic for doors/windows without OpenableObject
        VRHaptics.SendPattern(this, _controller, VRHaptics.BlockedDouble);
    }

    private void TryResolveDevice()
    {
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(handedness, devices);

        foreach (var d in devices)
        {
            // Make sure it's a valid, connected controller
            if (d.isValid)
            {
                _controller = d;
                _deviceResolved = true;
                return;
            }
        }
    }
}