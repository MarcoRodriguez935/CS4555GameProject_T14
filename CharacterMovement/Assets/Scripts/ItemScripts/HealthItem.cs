using UnityEngine;

public class HealthItem : MonoBehaviour
{

    public int healthBonus;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            Health playerHealth = other.GetComponent<Health>();

            if (playerHealth != null)
            {
                playerHealth.AddHealth(healthBonus);
                Debug.Log("Player gained: " + healthBonus + " hp");
                Destroy(gameObject);
            }
        }
    }
}
