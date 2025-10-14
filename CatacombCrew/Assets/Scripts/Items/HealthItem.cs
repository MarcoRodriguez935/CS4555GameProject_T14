using UnityEngine;

public class HealthItem : MonoBehaviour
{
    public int healAmount;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Health playerHealth = other.GetComponent<Health>();

            if (playerHealth != null)
            {
                playerHealth.AddHealth(healAmount);
                Debug.Log("Player healed by " + healAmount);

                Destroy(gameObject);
            }
        }
    }
}