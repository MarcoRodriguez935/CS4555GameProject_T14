using UnityEngine;
using UnityEngine.AI;

public class MutantLizardAnimation : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent navMeshAgent;

    void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        bool walking = navMeshAgent.velocity.magnitude > 0.01f && navMeshAgent.velocity.magnitude < 3.7f;
        animator.SetBool("Walking", walking);

        bool running = navMeshAgent.velocity.magnitude >= 3.7f;
        animator.SetBool("Running", running);
    }

    public void AttackAnim()
    {
        animator.SetTrigger("Attack");
    }
}
