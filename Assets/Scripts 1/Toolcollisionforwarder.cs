using UnityEngine;

/// <summary>
/// Attach this to every tool prefab alongside SchoolTool.
/// 
/// PURPOSE:
///   When the player swings a tool into a door or window, Unity fires OnTriggerEnter
///   on the TOOL's collider — not on the hand. This script catches that event and
///   forwards it to the OpenableObject on the door/window.
/// 
/// SETUP:
///   1. Add this component to the tool prefab (same GameObject as SchoolTool).
///   2. Make sure the tool has a Collider with IsTrigger = true.
///   3. No other configuration needed — it reads SchoolTool automatically.
/// 
/// NOTE ON COLLIDER LAYERS:
///   To avoid the tool trigger firing against the hand's own collider, put hands on
///   a "PlayerHand" layer and tools on a "Tool" layer, then use the Physics Layer
///   Collision Matrix (Edit → Project Settings → Physics) to uncheck
///   PlayerHand vs Tool.
/// </summary>
[RequireComponent(typeof(SchoolTool))]
[RequireComponent(typeof(Collider))]
public class ToolCollisionForwarder : MonoBehaviour
{
    // ─── Private state ────────────────────────────────────────────────────────

    // Cached reference — set once in Awake, never changes
    private SchoolTool _tool;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _tool = GetComponent<SchoolTool>();
    }

    // ─── Trigger detection ────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        // Only care about Door and Window tagged objects
        if (!other.CompareTag("Door") && !other.CompareTag("Window"))
            return;

        // If the tool isn't being held, ignore — dropped tools shouldn't open doors
        if (!_tool.holdingController.isValid)
            return;

        // Find the OpenableObject on the door/window
        var openable = other.GetComponent<OpenableObject>();
        if (openable == null)
        {
            // Door/window exists but has no OpenableObject → locked/jammed.
            // Give weak bump feedback so the player knows the tool reached it but can't open it.
            VRHaptics.SendPattern(this, _tool.holdingController, VRHaptics.WeakBump);
            return;
        }

        // Forward to OpenableObject — it decides what happens based on hot state + tool capability
        openable.OnToolContact(_tool, this);
    }
}