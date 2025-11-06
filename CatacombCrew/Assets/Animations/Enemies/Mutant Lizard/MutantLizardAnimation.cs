using UnityEngine;
using UnityEngine.AI;

public class MutantLizardAnimation : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private EnemyAttack enemyAttack;
    private float lastAttackTime = -999f;

    void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyAttack = GetComponent<EnemyAttack>();
    }

    void Update()
    {
        bool walking = navMeshAgent.velocity.magnitude > 0.01f && navMeshAgent.velocity.magnitude < 3.7f;
        animator.SetBool("Walking", walking);
        bool running = navMeshAgent.velocity.magnitude >= 3.7f;
        animator.SetBool("Running", running);

        if (enemyAttack != null)
        {
            if (enemyAttackTimeChanged())
            {
                animator.SetTrigger("Attack");
            }
        }

    }

    private bool enemyAttackTimeChanged()
    {
        var nextTime = enemyAttack.GetNextAttackTime();
        if (nextTime != lastAttackTime)
        {
            lastAttackTime = nextTime;
            return true;
        }
        return false;
    }

    public void AttackAnim()
    {
        animator.SetTrigger("Attack");
    }
}
