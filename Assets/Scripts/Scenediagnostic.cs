using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// SceneDiagnostic
// Attach to ANY GameObject in the scene and press Play.
// It prints a full report in the Console telling you exactly what is broken.
// Remove it once everything is fixed.
// ─────────────────────────────────────────────────────────────────────────────
public class SceneDiagnostic : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("===== SCENE DIAGNOSTIC START =====");

        // ── Event Systems ─────────────────────────────────────────────────────
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        Debug.Log($"[EventSystem] Found: {eventSystems.Length} (should be exactly 1)");
        foreach (var es in eventSystems)
        {
            Debug.Log($"  └─ '{es.gameObject.name}'");

            var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
                Debug.LogWarning($"     ⚠ Has StandaloneInputModule — XR needs XRUIInputModule instead. Remove this.");

            // Check for XR UI Input Module by name since it may not be in scope
            var modules = es.GetComponents<BaseInputModule>();
            bool hasXR = false;
            foreach (var m in modules)
            {
                string typeName = m.GetType().Name;
                Debug.Log($"     Input Module: {typeName}");
                if (typeName.Contains("XR")) hasXR = true;
            }
            if (!hasXR)
                Debug.LogWarning($"     ⚠ No XR Input Module found on EventSystem — UI ray interaction will not work.");
        }

        // ── Canvases ──────────────────────────────────────────────────────────
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"[Canvas] Found: {canvases.Length}");
        foreach (var c in canvases)
        {
            string mode = c.renderMode.ToString();
            bool hasGraphicRaycaster = c.GetComponent<GraphicRaycaster>() != null;

            // Check for TrackedDeviceGraphicRaycaster by name
            bool hasTrackedRaycaster = false;
            foreach (var comp in c.GetComponents<Component>())
                if (comp.GetType().Name.Contains("TrackedDevice")) hasTrackedRaycaster = true;

            Debug.Log($"  └─ '{c.gameObject.name}' | Mode: {mode} | GraphicRaycaster: {hasGraphicRaycaster} | TrackedDeviceRaycaster: {hasTrackedRaycaster}");

            if (c.renderMode == RenderMode.WorldSpace && !hasTrackedRaycaster)
                Debug.LogWarning($"     ⚠ World Space canvas needs TrackedDeviceGraphicRaycaster for XR ray interaction.");
            if (!hasGraphicRaycaster && !hasTrackedRaycaster)
                Debug.LogWarning($"     ⚠ Canvas has no raycaster at all — buttons will never receive clicks.");
        }

        // ── CompanionAI ───────────────────────────────────────────────────────
        if (CompanionAI.Instance == null)
            Debug.LogWarning("[CompanionAI] ⚠ No CompanionAI instance found — all voice lines will silently fail.");
        else
            Debug.Log($"[CompanionAI] ✓ Found on '{CompanionAI.Instance.gameObject.name}'");

        // ── Player tag ────────────────────────────────────────────────────────
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            Debug.LogWarning("[Player] ⚠ No GameObject tagged 'Player' found — triggers won't fire.");
        else
            Debug.Log($"[Player] ✓ Tagged Player: '{player.name}'");

        // ── Unlockable doors ──────────────────────────────────────────────────
        Unlockable[] doors = FindObjectsByType<Unlockable>(FindObjectsSortMode.None);
        Debug.Log($"[Unlockable] Found: {doors.Length} door(s)");
        foreach (var door in doors)
        {
            bool hasCollider = door.GetComponent<Collider>() != null;
            bool isTrigger = false;
            var col = door.GetComponent<Collider>();
            if (col != null) isTrigger = col.isTrigger;
            bool taggedDoor = door.CompareTag("Door");
            bool hasPivot = door.doorPivot != null;

            Debug.Log($"  └─ '{door.gameObject.name}' | Tagged Door: {taggedDoor} | HasCollider: {hasCollider} | IsTrigger: {isTrigger} | HasPivot: {hasPivot}");

            if (!hasCollider) Debug.LogWarning($"     ⚠ No collider — SchoolTool will never detect it.");
            if (!isTrigger) Debug.LogWarning($"     ⚠ Collider is not a trigger — set IsTrigger ON.");
            if (!hasPivot) Debug.LogWarning($"     ⚠ doorPivot not assigned — door won't animate.");
        }

        // ── SchoolTool ────────────────────────────────────────────────────────
        SchoolTool[] tools = FindObjectsByType<SchoolTool>(FindObjectsSortMode.None);
        Debug.Log($"[SchoolTool] Found: {tools.Length} (should be at least 1)");
        foreach (var t in tools)
        {
            bool hasCollider = t.GetComponent<Collider>() != null;
            bool hasRigidbody = t.GetComponent<Rigidbody>() != null;
            Debug.Log($"  └─ '{t.gameObject.name}' | HasCollider: {hasCollider} | HasRigidbody: {hasRigidbody}");
            if (!hasCollider) Debug.LogWarning($"     ⚠ No collider on ruler — can't detect door contact.");
            if (!hasRigidbody) Debug.LogWarning($"     ⚠ No Rigidbody — OnTriggerEnter requires at least one Rigidbody between the two objects.");
        }

        // ── ChoiceTrigger ─────────────────────────────────────────────────────
        ChoiceTrigger[] choices = FindObjectsByType<ChoiceTrigger>(FindObjectsSortMode.None);
        Debug.Log($"[ChoiceTrigger] Found: {choices.Length}");
        foreach (var c in choices)
        {
            bool hasCollider = c.GetComponent<Collider>() != null;
            bool isTrigger = false;
            var col = c.GetComponent<Collider>();
            if (col != null) isTrigger = col.isTrigger;
            bool hasPanel = c.choicePanel != null;

            Debug.Log($"  └─ '{c.gameObject.name}' | HasCollider: {hasCollider} | IsTrigger: {isTrigger} | HasPanel: {hasPanel}");
            if (!hasCollider) Debug.LogWarning($"     ⚠ No collider — zone will never trigger.");
            if (!isTrigger) Debug.LogWarning($"     ⚠ Collider is not a trigger.");
            if (!hasPanel) Debug.LogWarning($"     ⚠ choicePanel not assigned in Inspector.");
        }

        // ── Alarm ─────────────────────────────────────────────────────────────
        Alarminteractable[] alarms = FindObjectsByType<Alarminteractable>(FindObjectsSortMode.None);
        Debug.Log($"[Alarminteractable] Found: {alarms.Length}");
        foreach (var a in alarms)
            Debug.Log($"  └─ '{a.gameObject.name}'");

        Debug.Log("===== SCENE DIAGNOSTIC END =====");
    }
}