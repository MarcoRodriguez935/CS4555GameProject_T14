using UnityEngine;
using UnityEngine.AI;

public class LesserDemonAnimation : MonoBehaviour
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
        bool walking = navMeshAgent.velocity.magnitude > 0.01f;
        animator.SetBool("Walking", walking);
    }

    public void AttackAnim()
    {
        animator.SetTrigger("Attack");
    }

    public void Charging(bool charging)
    {
        animator.SetBool("Charging", charging);
    }
}