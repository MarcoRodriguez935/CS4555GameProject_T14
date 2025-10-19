using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent navMeshAgent;

    public float chaseDistance = 15f;

    public Transform[] patrolPoints;
    private int patrolIndex = 0;

    private float attackDistance = 2f;

    private EnemyAnimation enemyAn;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyAn = GetComponent<EnemyAnimation>();

        if (patrolPoints.Length > 0)
            navMeshAgent.SetDestination(patrolPoints[0].position);
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= chaseDistance)
        {
            navMeshAgent.SetDestination(player.position);
        }
        else
        {
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                navMeshAgent.SetDestination(patrolPoints[patrolIndex].position);
            }
        }

        if (distance <= attackDistance)
        {
            navMeshAgent.ResetPath();
            if (enemyAn != null)
                enemyAn.AttackAnim();
        }
        else if (distance <= chaseDistance)
        {
            navMeshAgent.SetDestination(player.position);
        }
        else
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            navMeshAgent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }
}