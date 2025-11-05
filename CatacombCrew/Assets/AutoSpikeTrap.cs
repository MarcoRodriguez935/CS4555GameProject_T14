using System.Collections;
using UnityEngine;

public class AutoSpikeTrap : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;
    public AnimatedSpikeTrap trapScript;

    [Tooltip("Time (in seconds) between spikes going up and down cycles.")]
    public float interval = 3f;

    [Tooltip("Delay before this trap starts activating (to offset multiple traps).")]
    public float startDelay = 0f;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (trapScript == null)
            trapScript = GetComponent<AnimatedSpikeTrap>();

        StartCoroutine(TrapCycle());
    }

    private IEnumerator TrapCycle()
    {
        yield return new WaitForSeconds(startDelay); // offset start if needed

        while (true)
        {
            // Raise spikes (trigger animation)
            animator.SetTrigger("open");
            trapScript?.SetSpikesUp(true);
            Debug.Log($"{name}: Spikes Up");

            yield return new WaitForSeconds(1f); // wait for the open animation length

            // Lower spikes (trigger animation)
            animator.SetTrigger("close");
            trapScript?.SetSpikesUp(false);
            Debug.Log($"{name}: Spikes Down");

            yield return new WaitForSeconds(interval); // wait before next raise
        }
    }
}