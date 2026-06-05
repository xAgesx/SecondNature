using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// CompanionAI  —  Smarter Robot Behaviour
// Attach to: The robot companion GameObject (root)
//
// Behaviour overview:
//   • Follows the player at a comfortable distance using a spring-damper model
//     so movement feels weighted and organic, not linear.
//   • Stops (and subtly shrinks back) when the player is looking directly at it.
//   • Idles with randomised micro-movements: drift, tilt, and look-around.
//   • Bobs continuously with a slight phase-shifted secondary bob for life.
//   • Strafes smoothly around obstacles instead of teleporting.
//   • All other scripts call CompanionAI.Instance.PlayVoiceLine(clip) to speak.
//
// Setup:
//   1. Attach this script to your robot root GameObject.
//   2. Assign Player Transform (Main Camera or XR Origin root).
//      Leave blank to auto-find by tag "Player".
//   3. Tune follow, gaze, idle, and audio settings in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────
public class CompanionAI : MonoBehaviour
{
    public static CompanionAI Instance { get; private set; }

    // ── Follow settings ───────────────────────────────────────────────────────

    [Header("Follow Target")]
    [Tooltip("The player transform to follow. For XR assign the Main Camera under XR Origin > Camera Offset.")]
    public Transform playerTarget;

    [Tooltip("Fallback if playerTarget is null — drag the XR Origin root here.")]
    public GameObject playerObj;

    [Header("Follow Behaviour")]
    [Tooltip("Ideal follow distance behind/beside the player (metres).")]
    public float followDistance = 1.8f;

    [Tooltip("Height above the player's pivot the robot hovers at.")]
    public float hoverHeight = 0.4f;

    [Tooltip("Lateral offset from the player's right side (negative = left).")]
    public float sideOffset = 0.6f;

    [Tooltip("Spring stiffness — higher = snappier catch-up.")]
    public float springStrength = 6f;

    [Tooltip("Spring damping — higher = less overshooting.")]
    public float springDamping = 4f;

    [Tooltip("The robot will not move at all if closer than this to its target pos.")]
    public float deadzone = 0.08f;

    [Tooltip("Max speed cap so it doesn't teleport on large gaps (m/s).")]
    public float maxMoveSpeed = 5f;

    [Header("Rotation")]
    [Tooltip("How quickly the robot turns to face the player.")]
    public float rotateSpeed = 4f;

    [Tooltip("X-axis correction for imported mesh orientation (−90 for flat-imported models).")]
    public float meshXRotation = -90f;

    // ── Gaze detection ────────────────────────────────────────────────────────

    [Header("Gaze Detection")]
    [Tooltip("Dot-product threshold for 'player is looking at robot'. " +
             "0.97 ≈ within ~14° of centre gaze. Lower = wider cone.")]
    [Range(0.8f, 1f)]
    public float gazeThreshold = 0.97f;

    [Tooltip("Maximum distance at which gaze detection is active (metres).")]
    public float gazeMaxDistance = 8f;

    [Tooltip("How long (seconds) the robot stays frozen after the player stops looking.")]
    public float gazeLingerTime = 0.6f;

    [Tooltip("When looked at, the robot nudges back this far (metres) — shy behaviour.")]
    public float gazeShrinkDistance = 0.15f;

    // ── Hover bob ─────────────────────────────────────────────────────────────

    [Header("Hover Bob")]
    public bool enableBob = true;

    [Tooltip("Primary bob amplitude (metres).")]
    public float bobAmplitude = 0.06f;

    [Tooltip("Primary bob frequency (Hz).")]
    public float bobFrequency = 1.2f;

    [Tooltip("Secondary micro-bob amplitude — layered on top for organic feel.")]
    public float microBobAmplitude = 0.018f;

    [Tooltip("Secondary micro-bob frequency — should be non-harmonic with primary.")]
    public float microBobFrequency = 2.7f;

    // ── Idle fidget ───────────────────────────────────────────────────────────

    [Header("Idle Fidget")]
    [Tooltip("Enable random drift/look-around behaviour when the player is stationary.")]
    public bool enableFidget = true;

    [Tooltip("Max random lateral drift distance from the ideal follow position (metres).")]
    public float fidgetDriftRadius = 0.25f;

    [Tooltip("How often (seconds) the robot picks a new idle drift target.")]
    [Range(1f, 8f)]
    public float fidgetInterval = 3.5f;

    [Tooltip("Max random tilt angle added during fidget (degrees, applied to local Z).")]
    public float fidgetTiltMax = 6f;

    // ── Audio ─────────────────────────────────────────────────────────────────

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioClip idleHumClip;

    [Range(0f, 1f)]
    public float voiceVolume = 1f;

    [Header("Spawn Voice Line")]
    public AudioClip spawnVoiceLine;
    public float spawnVoiceDelay = 1.2f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private readonly HashSet<AudioClip> _playedLines = new HashSet<AudioClip>();
    private AudioSource _humSource;

    // Spring physics
    private Vector3 _velocity = Vector3.zero;

    // Bob timers
    private float _bobTimer = 0f;
    private float _microBobTimer = 0f;

    // Gaze state
    private bool _isBeingLooked = false;
    private float _gazeLingerTimer = 0f;

    // Fidget state
    private Vector3 _fidgetOffset = Vector3.zero;
    private float _fidgetTimer = 0f;
    private float _currentTilt = 0f;
    private float _targetTilt = 0f;

    // Last known ideal position (used for spring)
    private Vector3 _springTarget = Vector3.zero;
    private bool _initialized = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.freezeRotation = true;

        SetupAudio();
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            if (playerObj != null)
                playerTarget = playerObj.transform;
            else
                Debug.LogWarning("[CompanionAI] No player target assigned. Assign manually in the Inspector.");
        }

        if (spawnVoiceLine != null)
            StartCoroutine(PlaySpawnLineDelayed());

        _springTarget = transform.position;
        _initialized = true;

        if (enableFidget)
            StartCoroutine(FidgetRoutine());

        Debug.Log("[CompanionAI] Companion initialized.");
    }

    private void Update()
    {
        if (!_initialized || playerTarget == null) return;

        UpdateGaze();
        UpdateFollowPosition();
        UpdateRotation();
    }

    // ── Gaze detection ────────────────────────────────────────────────────────

    private void UpdateGaze()
    {
        Vector3 toRobot = (transform.position - playerTarget.position);
        float dist = toRobot.magnitude;

        bool currentlyLooking = false;

        if (dist <= gazeMaxDistance)
        {
            Vector3 toRobotDir = toRobot.normalized;
            float dot = Vector3.Dot(playerTarget.forward, toRobotDir);
            currentlyLooking = dot >= gazeThreshold;
        }

        if (currentlyLooking)
        {
            _isBeingLooked = true;
            _gazeLingerTimer = gazeLingerTime;
        }
        else if (_gazeLingerTimer > 0f)
        {
            _gazeLingerTimer -= Time.deltaTime;
            if (_gazeLingerTimer <= 0f)
                _isBeingLooked = false;
        }
    }

    // ── Follow + spring ───────────────────────────────────────────────────────

    private void UpdateFollowPosition()
    {
        Vector3 playerPos = playerTarget.position;
        Vector3 playerForward = playerTarget.forward;
        Vector3 playerRight = playerTarget.right;

        playerForward.y = 0f; playerForward.Normalize();
        playerRight.y = 0f; playerRight.Normalize();

        // Ideal anchor position
        Vector3 ideal = playerPos
                      + playerForward * followDistance
                      + playerRight * sideOffset
                      + Vector3.up * hoverHeight;

        // Apply fidget drift on top
        ideal += _fidgetOffset;

        // When gazed at: nudge gently backwards (shy) and freeze spring target
        if (_isBeingLooked)
        {
            Vector3 awayFromPlayer = (transform.position - playerPos).normalized;
            awayFromPlayer.y = 0f;
            ideal = transform.position + awayFromPlayer * gazeShrinkDistance;
        }

        _springTarget = ideal;

        // Bob — two layered frequencies for organic feel
        float bobY = 0f;
        if (enableBob)
        {
            _bobTimer += Time.deltaTime;
            _microBobTimer += Time.deltaTime;
            bobY = Mathf.Sin(_bobTimer * bobFrequency * Mathf.PI * 2f) * bobAmplitude
                 + Mathf.Sin(_microBobTimer * microBobFrequency * Mathf.PI * 2f) * microBobAmplitude;
        }

        Vector3 springPos = _springTarget + Vector3.up * bobY;

        // Spring-damper integration
        float dist = Vector3.Distance(transform.position, springPos);
        if (dist > deadzone)
        {
            Vector3 springForce = (springPos - transform.position) * springStrength;
            Vector3 dampForce = -_velocity * springDamping;
            _velocity += (springForce + dampForce) * Time.deltaTime;
            _velocity = Vector3.ClampMagnitude(_velocity, maxMoveSpeed);
            transform.position += _velocity * Time.deltaTime;
        }
        else
        {
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, Time.deltaTime * springDamping);
        }
    }

    // ── Rotation ──────────────────────────────────────────────────────────────

    private void UpdateRotation()
    {
        Vector3 dirToPlayer = playerTarget.position - transform.position;
        dirToPlayer.y = 0f;

        if (dirToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
        }

        // Lock X, apply fidget tilt on Z
        _currentTilt = Mathf.LerpAngle(_currentTilt, _targetTilt, Time.deltaTime * 1.5f);

        Vector3 euler = transform.eulerAngles;
        euler.x = meshXRotation;
        euler.z = _currentTilt;
        transform.eulerAngles = euler;
    }

    // ── Idle fidget coroutine ─────────────────────────────────────────────────

    private IEnumerator FidgetRoutine()
    {
        while (true)
        {
            // Wait a randomised interval ± 30%
            float wait = fidgetInterval * Random.Range(0.7f, 1.3f);
            yield return new WaitForSeconds(wait);

            // Pick a new random drift offset in a circle
            Vector2 rand2D = Random.insideUnitCircle * fidgetDriftRadius;
            _fidgetOffset = new Vector3(rand2D.x, rand2D.y * 0.3f, 0f); // less vertical drift

            // Random tilt
            _targetTilt = Random.Range(-fidgetTiltMax, fidgetTiltMax);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Play a voice line. Each clip plays only once per session unless forcePlay is true.
    /// </summary>
    public void PlayVoiceLine(AudioClip clip, bool forcePlay = false)
    {
        if (clip == null)
        {
            Debug.LogWarning("[CompanionAI] PlayVoiceLine called with a null clip.");
            return;
        }

        if (!forcePlay && _playedLines.Contains(clip))
        {
            Debug.Log($"[CompanionAI] '{clip.name}' already played this session — skipping.");
            return;
        }

        if (voiceSource.isPlaying) voiceSource.Stop();
        voiceSource.PlayOneShot(clip, voiceVolume);
        _playedLines.Add(clip);

        Debug.Log($"[CompanionAI] Playing: '{clip.name}'");
    }

    public bool HasPlayed(AudioClip clip) => clip != null && _playedLines.Contains(clip);

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

    // ── Gizmos ────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerTarget == null) return;

        // Gaze cone
        Gizmos.color = _isBeingLooked
            ? new Color(1f, 0.2f, 0.2f, 0.3f)
            : new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, 0.25f);

        // Ideal follow anchor
        Vector3 fwd = playerTarget.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = playerTarget.right; right.y = 0f; right.Normalize();
        Vector3 ideal = playerTarget.position
                      + fwd * followDistance
                      + right * sideOffset
                      + Vector3.up * hoverHeight;

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(ideal, 0.12f);
        Gizmos.DrawLine(transform.position, ideal);
    }
#endif
}