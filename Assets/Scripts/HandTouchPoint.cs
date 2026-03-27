using UnityEngine;

// Auto-created child of HandContactReporter — do not add manually
public class HandTouchPoint : MonoBehaviour
{
    [HideInInspector] public HandContactReporter owner;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Door") && !other.CompareTag("Window")) return;
        owner?.ReportContact();
    }
}