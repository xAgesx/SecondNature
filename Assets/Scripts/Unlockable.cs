using UnityEngine;

// Attach this to any door tagged "Door" that the player can open with the ruler.
// When the ruler touches the door, it rotates open.
public class Unlockable : MonoBehaviour
{
    [Tooltip("The door panel Transform to rotate open (should pivot from the hinge edge).")]
    public Transform doorPivot;

    [Tooltip("How many degrees to rotate on Y when opening. Negative to flip direction.")]
    public float openAngle = 90f;

    [Tooltip("How long the open animation takes in seconds.")]
    public float openDuration = 0.4f;

    private bool _isOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        // Only respond if not already open
        if (_isOpen) return;

        // Check if the object that touched us has a SchoolTool component
        if (other.GetComponent<SchoolTool>() == null) return;

        _isOpen = true;
        StartCoroutine(AnimateOpen());
    }

    private System.Collections.IEnumerator AnimateOpen()
    {
        Quaternion startRot = doorPivot.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, openAngle, 0f);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            doorPivot.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        doorPivot.localRotation = endRot;
    }
}