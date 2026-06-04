using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// CompanionAI
// Attach to: The robot companion GameObject (root)
//
// Makes the robot float and smoothly follow the player at a set offset.
// Also acts as the central voice line manager for the whole companion system —
// all other scripts call CompanionAI.Instance.PlayVoiceLine(clip) to speak.
//
// Setup:
//   1. Attach this script to your robot root GameObject.
//   2. Assign the Player Transform — for XR Device Simulator, drag in the
//      Main Camera nested under XR Origin > Camera Offset.
//      Leave blank to auto-find by tag "Player".
//   3. Tune follow distance, height offset, and smoothing in the Inspector.
//   4. Assign a default AudioSource on the robot (or one will be created automatically).
//
// Fixes applied:
//   - Rigidbody forced kinematic + freeze rotation so mesh colliders don't block movement.
//   - Deadzone uses a flat 0.05 m threshold instead of the broken minDistance * 0.1f.
//   - X rotation locked to -90° every frame so the mesh stays upright.
//   - Bob timer uses unscaled sin to prevent drift over long sessions.
// ─────────────────────────────────────────────────────────────────────────────
public class CompanionAI : MonoBehaviour
{
    public static CompanionAI Instance { get; private set; }

    // ── Follow settings ───────────────────────────────────────────────────────

    [Header("Follow Target")]
    [Tooltip("The player transform to follow. For XR Device Simulator assign the Main Camera " +
             "under XR Origin > Camera Offset. Leave blank to auto-find by tag 'Player'.")]
    public Transform playerTarget;

    [Header("Follow Behaviour")]
    [Tooltip("How far behind/beside the player the robot hovers (metres).")]
    public float followDistance = 1.8f;

    [Tooltip("Height above the player's position the robot floats at.")]
    public float hoverHeight = 0.4f;

    [Tooltip("Horizontal offset to the side of the player (negative = left, positive = right).")]
    public float sideOffset = 0.6f;

    [Tooltip("How quickly the robot moves to its target position (position smoothing).")]
    public float moveSpeed = 3f;

    [Tooltip("How quickly the robot rotates to face the player.")]
    public float rotateSpeed = 4f;

    [Tooltip("Minimum distance from the desired follow position before the robot moves (metres). " +
             "Keeps it from jittering in place.")]
    public float minDistance = 0.05f;

    [Header("Mesh Rotation")]
    [Tooltip("X-axis rotation applied every frame to keep the mesh upright. " +
             "Set to -90 if your model was imported lying flat.")]
    public float meshXRotation = -90f;

    [Header("Hover Bob")]
    [Tooltip("Enable a gentle up/down floating animation.")]
    public bool enableBob = true;

    [Tooltip("How far up and down the robot bobs (metres).")]
    public float bobAmplitude = 0.06f;

    [Tooltip("How fast the bob cycle runs.")]
    public float bobFrequency = 1.2f;

    // ── Audio ─────────────────────────────────────────────────────────────────

    [Header("Audio")]
    [Tooltip("AudioSource used to play voice lines. Created automatically if left blank.")]
    public AudioSource voiceSource;

    [Tooltip("Optional: a short ambient hum/idle sound loop for the robot.")]
    public AudioClip idleHumClip;

    [Tooltip("Volume for voice lines (0–1).")]
    [Range(0f, 1f)]
    public float voiceVolume = 1f;

    // ── Spawn voice line ───────────────────────────────────────────────────────

    [Header("Spawn Voice Line")]
    [Tooltip("Voice line played once when the robot first spawns into the scene.")]
    public AudioClip spawnVoiceLine;

    [Tooltip("Delay in seconds before the spawn voice line plays.")]
    public float spawnVoiceDelay = 1.2f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private readonly HashSet<AudioClip> _playedLines = new HashSet<AudioClip>();

    private AudioSource _humSource;
    private float _bobTimer = 0f;
    private bool _initialized = false;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // ── FIX: This is a floating robot — gravity must be off entirely.
        //        Kinematic + no gravity means physics never touches the position;
        //        only this script moves it. If there is no Rigidbody we add one
        //        so there is a single guaranteed code path.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;   // ← key fix: gravity was pulling it down
        rb.isKinematic = true;    // physics engine won't push/pull it at all
        rb.freezeRotation = true;  // rotation is handled by UpdateRotation()

        SetupAudio();
    }

    private void Start()
    {
        // Auto-find player if not assigned
        if (playerTarget == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                playerTarget = playerObj.transform;
            else
                Debug.LogWarning("[CompanionAI] No player target assigned and no GameObject tagged " +
                                 "'Player' found. Assign the XR Camera manually in the Inspector.");
        }

        if (spawnVoiceLine != null)
            StartCoroutine(PlaySpawnLineDelayed());

        _initialized = true;
        Debug.Log("[CompanionAI] Robot companion initialized and ready.");
    }

    private void Update()
    {
        if (!_initialized || playerTarget == null) return;

        UpdateFollowPosition();
        UpdateRotation();
    }

    // ── Follow logic ──────────────────────────────────────────────────────────

    private void UpdateFollowPosition()
    {
        Vector3 playerPos = playerTarget.position;
        Vector3 playerForward = playerTarget.forward;
        Vector3 playerRight = playerTarget.right;

        // Flatten forward/right so the robot doesn't dive/climb with VR head tilt
        playerForward.y = 0f;
        playerRight.y = 0f;
        playerForward.Normalize();
        playerRight.Normalize();

        // Desired world position: behind + to the side + above the player
        Vector3 desiredPos = playerPos
                           - playerForward * followDistance
                           + playerRight * sideOffset
                           + Vector3.up * hoverHeight;

        // Bob offset — use a continuous timer so bob never drifts or resets
        if (enableBob)
        {
            _bobTimer += Time.deltaTime * bobFrequency;
            desiredPos.y += Mathf.Sin(_bobTimer * Mathf.PI * 2f) * bobAmplitude;
        }

        // ── FIX: flat deadzone (0.05 m) instead of the broken minDistance * 0.1f
        float dist = Vector3.Distance(transform.position, desiredPos);
        if (dist > minDistance)
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * moveSpeed);
    }

    private void UpdateRotation()
    {
        // Face the player on the Y axis only (no tilting toward/away)
        Vector3 dirToPlayer = playerTarget.position - transform.position;
        dirToPlayer.y = 0f;

        if (dirToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
        }

        // ── FIX: lock X rotation so the mesh stays upright (adjust meshXRotation in Inspector)
        Vector3 euler = transform.eulerAngles;
        euler.x = meshXRotation;
        transform.eulerAngles = euler;
    }

    // ── Public API — called by all trigger scripts ────────────────────────────

    /// <summary>
    /// Play a voice line clip. Will not replay a clip that has already been
    /// played this session (once-per-session guarantee).
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="forcePlay">If true, bypasses the once-per-session check.</param>
    public void PlayVoiceLine(AudioClip clip, bool forcePlay = false)
    {
        if (clip == null)
        {
            Debug.LogWarning("[CompanionAI] PlayVoiceLine called with a null clip.");
            return;
        }

        if (!forcePlay && _playedLines.Contains(clip))
        {
            Debug.Log($"[CompanionAI] Voice line '{clip.name}' already played this session — skipping.");
            return;
        }

        if (voiceSource.isPlaying)
            voiceSource.Stop();

        voiceSource.PlayOneShot(clip, voiceVolume);
        _playedLines.Add(clip);

        Debug.Log($"[CompanionAI] Playing voice line: '{clip.name}'");
    }

    /// <summary>
    /// Returns true if a clip has already been played this session.
    /// </summary>
    public bool HasPlayed(AudioClip clip) => clip != null && _playedLines.Contains(clip);

    /// <summary>
    /// Manually reset the played-lines history (e.g. on scene reload or checkpoint).
    /// </summary>
    public void ResetPlayedLines()
    {
        _playedLines.Clear();
        Debug.Log("[CompanionAI] Voice line history cleared.");
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private void SetupAudio()
    {
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.spatialBlend = 1f;
            voiceSource.rolloffMode = AudioRolloffMode.Linear;
            voiceSource.minDistance = 1f;
            voiceSource.maxDistance = 12f;
            voiceSource.playOnAwake = false;
        }

        if (idleHumClip != null)
        {
            _humSource = gameObject.AddComponent<AudioSource>();
            _humSource.clip = idleHumClip;
            _humSource.loop = true;
            _humSource.spatialBlend = 1f;
            _humSource.rolloffMode = AudioRolloffMode.Linear;
            _humSource.minDistance = 0.5f;
            _humSource.maxDistance = 5f;
            _humSource.volume = 0.3f;
            _humSource.Play();
        }
    }

    private IEnumerator PlaySpawnLineDelayed()
    {
        yield return new WaitForSeconds(spawnVoiceDelay);
        PlayVoiceLine(spawnVoiceLine);
    }
}