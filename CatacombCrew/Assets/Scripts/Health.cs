using UnityEngine;

public class Health : MonoBehaviour
{

    public float maxHealth = 100;
    private float currHealth;

    void Start()
    {
        currHealth = maxHealth;
    }

    public void TakeDamage(float damageTaken) 
    {
        currHealth -= damageTaken;
        Debug.Log(gameObject.name + " - " + damageTaken + " Current Health: " + currHealth);

        if (currHealth <= 0)
        {
            Die();
        }
    }

    public void AddHealth(float healthGained)
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
