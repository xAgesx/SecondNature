using UnityEngine;
using TMPro;

public class ChoiceTrigger : MonoBehaviour
{
    [Header("Choice Panel")]
    public GameObject choicePanel;

    [Header("Button Labels")]
    public TMP_Text buttonALabel;
    public TMP_Text buttonBLabel;
    public string optionAText = "Option A";
    public string optionBText = "Option B";

    [Header("Voice Lines")]
    public AudioClip optionAClip;
    public AudioClip optionBClip;

    [Header("Trigger")]
    [Tooltip("Tag on your XR Origin root.")]
    public string playerTag = "Player";
    public bool allowRepeat = false;

    private bool _shown = false;

    private void Start()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        if (buttonALabel != null) buttonALabel.text = optionAText;
        if (buttonBLabel != null) buttonBLabel.text = optionBText;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_shown && !allowRepeat) return;

        // Check the collider itself AND walk up the entire parent chain
        // so it works whether the collider is on the root or any child
        Transform t = other.transform;
        while (t != null)
        {
            if (t.CompareTag(playerTag))
            {
                ShowPanel();
                return;
            }
            t = t.parent;
        }
    }

    public void OnPickA() => PlayAndHide(optionAClip);
    public void OnPickB() => PlayAndHide(optionBClip);

    private void ShowPanel()
    {
        _shown = true;
        if (choicePanel != null) choicePanel.SetActive(true);
        Debug.Log("[ChoiceTrigger] Panel shown.");
    }

    private void PlayAndHide(AudioClip clip)
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        if (clip == null) { Debug.LogWarning("[ChoiceTrigger] No clip assigned."); return; }
        if (CompanionAI.Instance != null) CompanionAI.Instance.PlayVoiceLine(clip, true);
    }
}