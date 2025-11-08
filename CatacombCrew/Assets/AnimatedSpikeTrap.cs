using System.Collections.Generic;
using UnityEngine;

public class AnimatedSpikeTrap : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 10f;
    public float hitCooldown = 2f;
    [Tooltip("Only deal damage while spikes are raised.")]
    public bool onlyWhenUp = true;

    private Health playerHealth;

    private bool spikesUp = false;
    private Dictionary<int, float> lastHitTimeById = new Dictionary<int, float>();

    // Called externally by other scripts
    public void SetSpikesUp(bool up)
    {
        spikesUp = up;
    }

    private void OnTriggerStay(Collider other)
    {
        if (onlyWhenUp && !spikesUp) return;
        if (!other.CompareTag("Player")) return;

        int id = other.GetInstanceID();
        float last;
        lastHitTimeById.TryGetValue(id, out last);
        if (Time.time - last < hitCooldown) return;

        var stats = other.GetComponent<PlayerStats>();
        playerHealth = other.GetComponent<Health>();
        if (stats != null)
        {
            stats.TakeDamage(damage);
            playerHealth.TakeDamage(damage);
            Debug.Log($"{name} dealt {damage} damage to {other.name} (spikesUp={spikesUp})");
        }

        lastHitTimeById[id] = Time.time;
    }
}