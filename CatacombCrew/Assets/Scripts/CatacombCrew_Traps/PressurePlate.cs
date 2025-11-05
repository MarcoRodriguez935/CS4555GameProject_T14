using System.Collections;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Linked Trap(s)")]
    public Animator[] linkedTrapAnimators;
    public AnimatedSpikeTrap[] linkedTrapScripts;

    [Header("Plate Settings")]
    public float pressDownAmount = 0.1f;
    public float pressSpeed = 4f;
    public float releaseDelay = 1f;

    private Vector3 originalPosition;
    private bool pressed = false;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !pressed)
        {
            pressed = true;
            StopAllCoroutines();
            StartCoroutine(MovePlate(originalPosition - new Vector3(0, pressDownAmount, 0)));

            // Play animations
            foreach (var anim in linkedTrapAnimators)
            {
                anim.SetTrigger("open");
            }

            // Tell spike scripts they're up (for damage)
            foreach (var trap in linkedTrapScripts)
            {
                trap.SetSpikesUp(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            StartCoroutine(ReleasePlate());
    }

    private IEnumerator ReleasePlate()
    {
        yield return new WaitForSeconds(releaseDelay);
        StartCoroutine(MovePlate(originalPosition));

        // Close animation
        foreach (var anim in linkedTrapAnimators)
        {
            anim.SetTrigger("close");
        }

        // Turn off damage
        foreach (var trap in linkedTrapScripts)
        {
            trap.SetSpikesUp(false);
        }

        pressed = false;
    }

    private IEnumerator MovePlate(Vector3 target)
    {
        Vector3 start = transform.localPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * pressSpeed;
            transform.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }
}