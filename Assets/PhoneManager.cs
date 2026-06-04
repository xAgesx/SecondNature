using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneManager : MonoBehaviour
{
    public string currentNumber = "";
    public string targetNumber;
    public Text displayNumberTxt;

    [Header("Outline")]
    public Image outline;
    public Color oulineDefault;
    public Color outlineFalse;
    public Color outlineTrue;

    [Header("Phone Voice Lines")]
    [Tooltip("The emergency responder audio clip (MP3 from Assets/Sounds).")]
    public AudioClip responderClip;

    [Tooltip("The companion NPC voice line that plays after the responder finishes.")]
    public AudioClip companionClip;

    private bool _successTriggered = false;

    public void onClickPhoneBtn(float digit)
    {
        outline.color = oulineDefault;

        if (digit == -1)
        {
            if (currentNumber.Length > 0)
                currentNumber = currentNumber.Substring(0, currentNumber.Length - 1);
            updatePhoneDisplay(currentNumber);
            return;
        }

        if (currentNumber.Length >= 3)
        {
            outline.color = outlineFalse;
            return;
        }

        currentNumber += digit.ToString();
        updatePhoneDisplay(currentNumber);

        if (currentNumber.Length == targetNumber.Length && currentNumber != targetNumber)
        {
            outline.color = outlineFalse;
            return;
        }

        if (currentNumber == targetNumber && !_successTriggered)
        {
            _successTriggered = true;
            outline.color = outlineTrue;
            Debug.Log("[PhoneManager] Correct number dialled — starting voice chain.");
            StartCoroutine(PlayVoiceChain());
        }
    }

    private IEnumerator PlayVoiceChain()
    {
        // Play responder clip directly on CompanionAI's voice source
        if (responderClip != null && CompanionAI.Instance != null)
        {
            CompanionAI.Instance.PlayVoiceLine(responderClip, true);
            // Wait for it to finish
            yield return new WaitForSeconds(responderClip.length + 0.2f);
        }

        // Play companion follow-up
        if (companionClip != null && CompanionAI.Instance != null)
            CompanionAI.Instance.PlayVoiceLine(companionClip, true);
    }

    public void updatePhoneDisplay(string localNumber)
    {
        string display = localNumber;
        while (display.Length < 3)
            display += "-";
        displayNumberTxt.text = display;
    }
}