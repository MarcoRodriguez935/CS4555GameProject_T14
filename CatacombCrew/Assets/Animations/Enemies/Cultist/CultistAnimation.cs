using UnityEngine;
using UnityEngine.AI;

public class CultistAnimation : MonoBehaviour
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
        bool walking = navMeshAgent.velocity.magnitude > 0.1f;
        animator.SetBool("Walking", walking);
    }

    public void SummonAnim()
    {
        animator.SetTrigger("Summon");
    }
}