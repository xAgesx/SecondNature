
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LeverLogic : MonoBehaviour {
    public HingeJoint hinge;
    public float threshold = 35f; 
    public UnityEvent onLeverPulled;
    private bool isActivated = false;
    public GameObject toBeContinued;

    void Update() {
        if (!isActivated && hinge.angle >= threshold) {
            isActivated = true;
            onLeverPulled.Invoke();
            Debug.Log("Lever Activated!");
        } else if (isActivated && hinge.angle < threshold - 5f) {
            isActivated = false;
        }
    }
    public void displayCanvas() {
        StartCoroutine(coroutine1());
    }
    public IEnumerator coroutine1() {
        yield return new WaitForSeconds(3);
        toBeContinued.SetActive(true);
    }
}