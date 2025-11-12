using UnityEngine;

public class DoorUnlockManager : MonoBehaviour
{
    public LeverSwitch[] levers; // Assign all 4 levers in the inspector
    public bool allLeversFlipped = false;

    [Header("Door Settings")]
    public Animator doorAnimator;
    public Collider doorCollider;
    public AudioSource unlockSound;

    public void CheckAllLevers()
    {
        // Check if all levers are flipped
        foreach (LeverSwitch lever in levers)
        {
            if (lever == null || !lever.isFlipped)
            {
                allLeversFlipped = false;
                return;
            }
        }

        // All levers flipped!
        allLeversFlipped = true;
        UnlockDoor();
    }

    private void UnlockDoor()
    {
        if (unlockSound != null)
            unlockSound.Play();

        if (doorAnimator != null)
            doorAnimator.SetTrigger("Unlock");

        if (doorCollider != null)
            doorCollider.enabled = false; // Allow player through

        Debug.Log(" All levers flipped! Door unlocked.");
    }
}