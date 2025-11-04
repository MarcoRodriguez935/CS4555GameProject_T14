using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public float attackRange = 2f;
    public int attackDamage = 20;
    public float attackCoolDown = 1.5f;

    private float nextAttackTime = 0f;
    private Transform player;
    private Health playerHealth;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<Health>();
        }
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCoolDown;
        }
    }

    public void Attack()
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name} attacked {player.name} for {attackDamage} damage!");
        }
    }

    public void Attack2(int damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Debug.Log($"{gameObject.name} attacked {player.name} for {damage} damage!");
        }
    }
}