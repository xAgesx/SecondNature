using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Static utility for sending haptic patterns to XR controllers.
/// Works with OpenXR / XR Interaction Toolkit.
/// </summary>
public static class VRHaptics
{
    // ─── Haptic pattern definitions ───────────────────────────────────────────
    // Each pattern is a series of (amplitude 0-1, duration seconds) pairs.
    // Amplitude: 0 = off, 1 = max vibration.

    // "No" signal: two short sharp buzzes
    // Fires when bare hand touches a door or window → you can't open this bare-handed
    public static readonly HapticPulse[] BlockedDouble = new HapticPulse[]
    {
        new HapticPulse(0.8f, 0.08f),   // buzz on
        new HapticPulse(0.0f, 0.04f),   // gap
        new HapticPulse(0.8f, 0.08f),   // buzz on again
    };

    // "Click" signal: one strong hit that tapers down — feels like a door latch
    // Fires when a tool successfully opens a door or window
    public static readonly HapticPulse[] SuccessClick = new HapticPulse[]
    {
        new HapticPulse(1.0f, 0.12f),   // strong initial hit
        new HapticPulse(0.5f, 0.05f),   // half strength fade
        new HapticPulse(0.15f, 0.04f),  // trailing off
    };

    // "Heat warning" signal: rapid oscillation — feels like something pulsing with heat
    // Fires when a tool touches a hot door or window
    public static readonly HapticPulse[] HeatWarning = new HapticPulse[]
    {
        new HapticPulse(0.6f, 0.05f),
        new HapticPulse(0.0f, 0.03f),
        new HapticPulse(0.6f, 0.05f),
        new HapticPulse(0.0f, 0.03f),
        new HapticPulse(0.6f, 0.05f),
        new HapticPulse(0.0f, 0.03f),
        new HapticPulse(0.6f, 0.05f),
    };

    // "Weak tool" signal: one soft single bump — "that didn't work"
    // Fires when a tool isn't capable enough (e.g. ruler on window)
    public static readonly HapticPulse[] WeakBump = new HapticPulse[]
    {
        new HapticPulse(0.3f, 0.10f),
    };

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Send a haptic pattern to a specific XR controller.
    /// Needs a MonoBehaviour to run the coroutine — pass the calling component.
    /// </summary>
    /// <param name="runner">Any active MonoBehaviour to host the coroutine.</param>
    /// <param name="device">The XR input device (left or right controller).</param>
    /// <param name="pattern">One of the static pattern arrays above.</param>
    public static void SendPattern(MonoBehaviour runner, InputDevice device, HapticPulse[] pattern)
    {
        runner.StartCoroutine(PlayPattern(device, pattern));
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    private static IEnumerator PlayPattern(InputDevice device, HapticPulse[] pattern)
    {
        foreach (var pulse in pattern)
        {
            if (pulse.amplitude > 0f)
            {
                // SendHapticImpulse(channel, amplitude, duration)
                // Channel 0 = default haptic motor
                device.SendHapticImpulse(0, pulse.amplitude, pulse.duration);
            }
            // Always wait the full pulse duration before the next one
            yield return new WaitForSeconds(pulse.duration);
        }
    }
}

/// <summary>
/// One step in a haptic pattern: how hard and how long.
/// </summary>
[System.Serializable]
public struct HapticPulse
{
    /// <summary>Vibration strength, 0 (silent) to 1 (max).</summary>
    public float amplitude;
    /// <summary>How long this pulse lasts in seconds.</summary>
    public float duration;

    public HapticPulse(float amplitude, float duration)
    {
        this.amplitude = amplitude;
        this.duration = duration;
    }
}