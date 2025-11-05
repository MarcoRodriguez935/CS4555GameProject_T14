using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpikeTrap : MonoBehaviour
{
   [Header("Spike movement")]
   public Transform spikeTransform;         // the child transform that moves (the spikes mesh)
   public Vector3 upLocalPosition = new Vector3(0, 0.5f, 0);    // local pos when spikes up
   public Vector3 downLocalPosition = new Vector3(0, -0.2f, 0); // local pos when spikes hidden
   public float moveSpeed = 6f;             // how fast spikes move
   public bool startDown = true;            // start hidden

   [Header("Damage")]
   public float damage = 25f;
   public float hitCooldown = 1f;           // seconds before the same player can be damaged again

    // internal
    Coroutine moveRoutine;
    Dictionary<int, float> lastHitTimeById = new Dictionary<int, float>();

    void Reset()
    {
        // ensure we have a collider on the root (used for editor convenience)
        var c = GetComponent<Collider>();
        c.isTrigger = false;
    }

    void Start()
    {
        if (spikeTransform == null && transform.childCount > 0)
            spikeTransform = transform.GetChild(0);

        spikeTransform.localPosition = startDown ? downLocalPosition : upLocalPosition;
    }

    // call to raise spikes
    public void Raise()
    {
        StartMove(upLocalPosition);
    }

    // call to lower spikes
    public void Lower()
    {
        StartMove(downLocalPosition);
    }

    void StartMove(Vector3 targetLocal)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveToLocal(spikeTransform, targetLocal));
    }

    IEnumerator MoveToLocal(Transform t, Vector3 targetLocal)
    {
        Vector3 start = t.localPosition;
        float dist = Vector3.Distance(start, targetLocal);
        float time = dist / Mathf.Max(0.001f, moveSpeed);
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / time);
            t.localPosition = Vector3.Lerp(start, targetLocal, alpha);
            yield return null;
        }

        t.localPosition = targetLocal;
        moveRoutine = null;
    }

    // This collider (a separate trigger child) will do damage; damage logic can also be attached to the moving spike child as trigger.
    // But here OnTriggerEnter is implemented to allow spikes to damage players (spike root must have a trigger child or spikeTransform must have a BoxCollider set as Trigger.)
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        int id = other.GetInstanceID();
        float last;
        lastHitTimeById.TryGetValue(id, out last);
        if (Time.time - last < hitCooldown) return;

        // apply damage if PlayerStates exists
        var stats = other.GetComponent<PlayerStats>();
        if (stats != null)
            stats.TakeDamage(damage);
        
        lastHitTimeById[id] = Time.time;
    }
}
