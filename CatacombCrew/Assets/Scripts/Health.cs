using UnityEngine;

public class Health : MonoBehaviour
{

    public PlayerSounds sound;

    public float maxHealth = 100;
    private float currHealth;

    void Start()
    {

        sound = GetComponent<PlayerSounds>();
        currHealth = maxHealth;
    }

    public void TakeDamage(float damageTaken) 
    {
        currHealth -= damageTaken;
        Debug.Log(gameObject.name + " - " + damageTaken + " Current Health: " + currHealth);

        sound.PlayHurt();

        if (currHealth <= 0)
        {
            StartCoroutine(Die());
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

    public System.Collections.IEnumerator Die() 
    {
        sound.PlayDeath();
        Debug.Log(gameObject.name + " died!");
        yield return new WaitForSeconds(1.50f);
        gameObject.SetActive(false);
    }
}
