using UnityEngine;
using System.Collections;

public class SpikeTrap : MonoBehaviour
{
    [Header("References")]
    public Transform spikes; // assign the Spikes object here
    public Transform pressurePlate; // assign plate if you want it to animate
    public string playerTag = "Player";

    [Header("Spike Settings")]
    public Vector3 upOffset = new Vector3(0, 2f, 0); // how far spikes rise
    public float riseTime = 0.3f;   // speed going up
    public float stayUpTime = 1f;   // how long spikes stay up
    public float resetTime = 0.5f;  // speed going back down

    private Vector3 startPos;
    private bool isActive = false;

    void Start()
    {
        startPos = spikes.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive && other.CompareTag(playerTag))
        {
            StartCoroutine(ActivateTrap());
        }
    }

    private IEnumerator ActivateTrap()
    {
        isActive = true;

        // Spikes up
        yield return MoveSpikes(startPos, startPos + upOffset, riseTime);

        // Wait while up
        yield return new WaitForSeconds(stayUpTime);

        // Spikes down
        yield return MoveSpikes(spikes.localPosition, startPos, resetTime);

        isActive = false; // reset, ready for next activation
    }

    private IEnumerator MoveSpikes(Vector3 from, Vector3 to, float duration)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            spikes.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }
        spikes.localPosition = to;
    }
}