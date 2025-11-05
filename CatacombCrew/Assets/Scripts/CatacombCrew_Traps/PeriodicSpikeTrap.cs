using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpikeTrap))]
public class PeriodicSpikeTrap : MonoBehaviour
{
    public float upTime = 1.0f;     // how long spikes stay up
    public float downTime = 2.0f;   // how long spikes stay down
    public float initialDelay = 0.0f;
    public bool startDown = true;

    SpikeTrap spike;

    void Start()
    {
        spike = GetComponent<SpikeTrap>();
        spike.spikeTransform.localPosition = startDown ? spike.downLocalPosition : spike.upLocalPosition;
        StartCoroutine(RunLoop());
    }

    IEnumerator RunLoop()
    {
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            if (startDown)
            {
                // wait down, then raise
                yield return new WaitForSeconds(downTime);
                spike.Raise();
                yield return new WaitForSeconds(upTime);
                spike.Lower();
            }
            else
            {
                // start up
                spike.Raise();
                yield return new WaitForSeconds(upTime);
                spike.Lower();
                yield return new WaitForSeconds(downTime);
            }
        }
    }
}
