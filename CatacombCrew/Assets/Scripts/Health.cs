using UnityEngine;

public class Health : MonoBehaviour
{

    public int maxHealth = 100;
    private int currHealth;

    void Start()
    {
        currHealth = maxHealth;
    }

    public void TakeDamage(int damageTaken) 
    {
        currHealth -= damageTaken;
        Debug.Log(gameObject.name + " - " + damageTaken + " Current Health: " + currHealth);

        if (currHealth <= 0)
        {
            Die();
        }
    }

    public void AddHealth(int healthGained)
    {
        currHealth += healthGained;
        if (currHealth > 100) {
            currHealth = 100;
        }
        Debug.Log("Current Health: " + currHealth);
    }

    public void Die() 
    {
        Debug.Log(gameObject.name + " died!");
        Destroy(gameObject);
    }
}
