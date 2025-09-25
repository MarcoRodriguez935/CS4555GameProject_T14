using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    public float attackRange = 2f;
    public int attackDamage = 20;
    public float attackCoolDown = 0.5f;

    private float nextAttackTime = 0f;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey("f") && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCoolDown;
        }
    }

    void Attack()
    {
        RaycastHit hit;

        if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out hit, attackRange))
        {
            Health enemyHealth = hit.collider.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
                Debug.Log($"Player hit {hit.collider.name} for {attackDamage} damage!");
            }
        }
    }
}
