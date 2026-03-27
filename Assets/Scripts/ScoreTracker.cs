using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ScoreTracker
// Attach to: XR Rig root (same object as TemperatureSystem)
//
// Tracks live stats during play. Call CompleteObjective() from any task
// script when the player finishes an objective.
// ─────────────────────────────────────────────────────────────────────────────
public class ScoreTracker : MonoBehaviour
{
    public static ScoreTracker Instance { get; private set; }

    [Header("Objectives")]
    [Tooltip("Total number of objectives in this scene.")]
    public int totalObjectives = 3;

    public int ErrorCount { get; private set; } = 0;
    public int CompletedObjectives { get; private set; } = 0;
    public float ElapsedTime { get; private set; } = 0f;
    public bool IsTracking { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (IsTracking)
            ElapsedTime += Time.deltaTime;
    }

    /// <summary>Call once per damage event (not per tick).</summary>
    public void RegisterError()
    {
        if (!IsTracking) return;
        ErrorCount++;
    }

    /// <summary>Call this from any task/objective script when player completes a goal.</summary>
    public void CompleteObjective()
    {
        if (!IsTracking) return;
        CompletedObjectives = Mathf.Clamp(CompletedObjectives + 1, 0, totalObjectives);
    }

    public void StopTracking() => IsTracking = false;
}