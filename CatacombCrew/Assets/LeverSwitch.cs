using UnityEngine;

public class LeverSwitch : MonoBehaviour
{
    public bool isFlipped = false;
    public Animator animator;
    public AudioSource flipSound;
    public DoorUnlockManager doorManager; // Reference to the central manager

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void FlipLever()
    {
        if (isFlipped) return; // Don’t flip twice
        isFlipped = true;

        if (animator != null)
            animator.SetTrigger("Flip");

        if (flipSound != null)
            flipSound.Play();

        if (doorManager != null)
            doorManager.CheckAllLevers();
    }
}