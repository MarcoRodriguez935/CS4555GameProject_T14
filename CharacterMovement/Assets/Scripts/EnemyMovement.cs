// Building off of the script provided in the Rollaball tutorial
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public Transform player;
    private NavMeshAgent navMeshAgent;

    // How far the enemy can detect the player
    public float chaseDistance = 15f;

    // Different points within the map where enemy patrols while not chasing
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
            // Chase the player
            navMeshAgent.SetDestination(player.position);
        }
        else
        {
            // Patrol
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                navMeshAgent.SetDestination(patrolPoints[patrolIndex].position);
            }
        }

        // If enemy is right next to player (Attack)
        if (distance <= attackDistance) 
        {
            navMeshAgent.ResetPath();
            if (enemyAn != null)
                enemyAn.AttackAnim();

            Debug.Log("Enemy Attack");
        }
        // Chase after player while in range
        else if (distance <= chaseDistance) 
        {
            navMeshAgent.SetDestination(player.position);
        }
        // Resume patrolling
        else 
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            navMeshAgent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }
}
