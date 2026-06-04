using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// Alarminteractable
// Works with Meta XRI (Oculus Interaction SDK).
//
// Setup:
//   1. Attach this to the lever/alarm GameObject.
//   2. On the same GameObject's GrabInteractable, find "Interactable Unity Events"
//      → WhenSelectingInteractorViewAdded → click + → drag THIS GameObject here
//      → function: Alarminteractable.Trigger
// ─────────────────────────────────────────────────────────────────────────────
public class Alarminteractable : MonoBehaviour
{
    [Header("Alarm Sound")]
    [Tooltip("The alarm audio clip to play when triggered.")]
    public AudioClip alarmClip;

    [Tooltip("How many seconds the alarm plays before the NPC speaks.")]
    public float alarmDuration = 3f;

    [Header("NPC Voice Line")]
    [Tooltip("Companion NPC voice line that plays after the alarm stops.")]
    public AudioClip companionClip;

    private bool _triggered = false;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
            _audioSource.playOnAwake = false;
        }
    }

    // Called from GrabInteractable UnityEvent in the Inspector
    public void Trigger()
    {
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(AlarmThenSpeak());
        Debug.Log("[Alarminteractable] Triggered.");
    }

    private IEnumerator AlarmThenSpeak()
    {
        if (alarmClip != null)
        {
            _audioSource.clip = alarmClip;
            _audioSource.loop = true;
            _audioSource.Play();
            yield return new WaitForSeconds(alarmDuration);
            _audioSource.Stop();
            _audioSource.loop = false;
        }

        if (companionClip != null && CompanionAI.Instance != null)
            CompanionAI.Instance.PlayVoiceLine(companionClip, false);

        Debug.Log("[Alarminteractable] Sequence complete.");
    }
}