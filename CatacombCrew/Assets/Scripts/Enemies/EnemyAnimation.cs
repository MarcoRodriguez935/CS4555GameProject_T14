using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimation : MonoBehaviour
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

    }
}