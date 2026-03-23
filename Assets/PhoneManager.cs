using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneManager : MonoBehaviour {
    public string currentNumber = "";
    public string targetNumber;
    public Text displayNumberTxt;
    [Header("Outline")]
    public Image outline;
    public Color oulineDefault;
    public Color outlineFalse;
    public Color outlineTrue;

    public void onClickPhoneBtn(float digit) {
        outline.color = oulineDefault;

        if (digit == -1) {
            if (currentNumber.Length > 0) {
                currentNumber = currentNumber.Substring(0, currentNumber.Length - 1);
            }
            updatePhoneDisplay(currentNumber);
            return;
        }

        if (currentNumber.Length >= 3) {
            outline.color = outlineFalse;
            return;
        }
        

        currentNumber += digit.ToString();
        updatePhoneDisplay(currentNumber);
        
        if(currentNumber.Length == targetNumber.Length && currentNumber != targetNumber) {
            outline.color = outlineFalse;
            return;
        }
        else if (currentNumber == targetNumber) {
            outline.color = outlineTrue;
            Debug.Log("Phone Dialed Successfully");
        }
    }

    public void updatePhoneDisplay(string localNumber) {
        string display = localNumber;
        while (display.Length < 3) {
            display += "-";
        }
        displayNumberTxt.text = display;
    }
}