using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class UndeadGuards : EnemyBase
{

    /*  The Undead Guards are intelligent patrol enemies that patrol rooms (get points from a room object, patrol it for a while, then move to the next room)
        if there are no rooms the guards will simply follow a regular patrol route assigned to them
        If they see the players, they will give chase and attempt to intercept them using group tactics to surround them.
        if they hear the players, they will pair up and investigate opposite sides of the sounds (left and right side of the source)
        when one sees the players, he alerts his partner; 
        Guards can also be alerted by watchtowers (replace onsound with a chasing method inside watchtower), and called
            upon by the king when he hears a sound.
        When the players are nearby, the guards will poke at them with their spears and give chase
        if the players are far and being chased, the guards should be fed their location until they lose los
            when they lose los they should pair up and search the area (again on opposite sides)
            when chasing, they should attempt to cut them off. 
    */

    public List<Transform> roomPoints = new List<Transform>();
    public List<Transform> patrolPoints = new List<Transform>();

    private float patrolSpeed = 3.2f;
    private float chaseSpeed = 4.6f;
    private float pokeRange = 1.75f;
    private float interceptLead = 2f;
    private float loseInterestAfter = 6f;
    private float searchTime = 5f;

    enum GuardState { Patrol, Chase, Investigate, Search }
    GuardState state = GuardState.Patrol;

    private int patrolDest;
    private float interestUntil;
    private float searchUntil;

    private Transform playerLock;
    private Rigidbody playerBody;
    private Vector3 lastKnownPos;

    static int sInvestigateCounter = 0;

    public override void Awake(){
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.autoRepath = true;
        agent.autoBraking = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.acceleration = Mathf.Max(agent.acceleration, 12f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 540f);
        agent.stoppingDistance = 0.25f;
        
        if(eyes == null) eyes = transform;
        sightDistance = Mathf.Max(sightDistance, 14f);
    }

    public void Start(){
        EnsureOnMesh(3f);
        agent.speed = patrolSpeed;
        NextPatrolPoint();
    }

    public override void Update(){
        base.Update();

        if(!EnsureOnMesh(3f)) return;

        switch(state){
            case GuardState.Patrol:
                Patrol();
                break;

            case GuardState.Chase:
                Chase();
                break;

            case GuardState.Investigate:
                Investigate();
                break;

            case GuardState.Search:
                Search();
                break;
        }
    }

    void Patrol(){
        agent.speed = patrolSpeed;
        if(!agent.hasPath || agent.remainingDistance <= 0.4f){
            NextPatrolPoint();
        }
    }

    void Chase(){
        agent.speed = chaseSpeed;

        if(playerLock != null){
            Vector3 lead = Vector3.zero;
            if(playerBody != null){
                var vel = playerBody.linearVelocity;
                if(vel.sqrMagnitude > 0.01f){
                    lead = vel.normalized * interceptLead;
                }
            }
            lastKnownPos = playerLock.position + lead;
        }


        Vector3 toTarget = lastKnownPos - transform.position;
        toTarget.y = 0f;
        Vector3 side = toTarget.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, toTarget.normalized) : transform.right;
        int laneIndex = (gameObject.GetInstanceID() & 3) - 1;
        float laneWidth = 0.9f;
        Vector3 chaseTarget = lastKnownPos + side * (laneIndex * laneWidth);

        agent.SetDestination(chaseTarget);

        if(Vector3.SqrMagnitude(transform.position - lastKnownPos) <= pokeRange * pokeRange){
            //stab animation
            //damage player
        }

        if(Time.time > interestUntil){
            state = GuardState.Search;
            searchUntil = Time.time + searchTime;
            SearchPair(lastKnownPos);
        }
    }

    void Investigate(){
        agent.speed = patrolSpeed + 0.4f;
        if(agent.remainingDistance <= 0.5f){
            state = GuardState.Search;
            searchUntil = Time.time + searchTime;
        }
    }

    void Search(){
        if(Time.time > searchUntil){
            state = GuardState.Patrol;
            playerLock = null;
            playerBody = null;
            NextPatrolPoint();
            return;
        }

        if(!agent.hasPath || agent.remainingDistance <= 0.5f){
            Vector3 point = lastKnownPos + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
            NavMeshHit hit;
            if(NavMesh.SamplePosition(point, out hit, 2f, NavMesh.AllAreas)){
                agent.SetDestination(hit.position);
            }
        }
    }   

    public override void OnSeen(Vector3 origin, Rigidbody body){
        base.OnSeen(origin, body);
        playerLock = body ? body.transform : playerLock;
        playerBody = body != null ? body : playerBody;
        lastKnownPos = origin;
        interestUntil = Time.time + loseInterestAfter;

        state = GuardState.Chase;
        agent.isStopped = false;
    }

    public override void OnSound(Vector3 origin, Vector3 direction, float magnitude, GameObject reason){
        base.OnSound(origin, direction, magnitude, reason);

        if(state == GuardState.Chase){
            lastKnownPos = origin;
            interestUntil = Time.time + loseInterestAfter;
            return;
        }

        if(reason && (reason.GetComponent<Watchtower>() != null || reason.GetComponent<CorruptedKing>() != null)){
            state = GuardState.Chase;
            lastKnownPos = origin;
            interestUntil = Time.time + loseInterestAfter;
            agent.isStopped = false;
            return;
        }

        state = GuardState.Investigate;
        lastKnownPos = origin;
        InvestigatePair(origin);

    }

    void NextPatrolPoint(){
        var list = roomPoints != null && roomPoints.Count > 0 ? roomPoints : patrolPoints;
        if(list == null || list.Count == 0){
            agent.ResetPath();
            return;
        }
        patrolDest = (patrolDest + 1) % list.Count;
        var t = list[patrolDest];
        if(t == null){
            agent.ResetPath();
            return;
        }

        NavMeshHit hit;
        if(NavMesh.SamplePosition(t.position, out hit, 2f, NavMesh.AllAreas)){
            agent.SetDestination(hit.position);
        }
    }

    void InvestigatePair(Vector3 origin){
        int side = (++sInvestigateCounter & 1) == 0 ? 1 : -1;
        Vector3 toGuard = (transform.position - origin);
        toGuard.y = 0f;
        Vector3 right = Vector3.Cross(Vector3.up, toGuard.normalized);
        Vector3 offset = right * (2f * side) + toGuard.normalized * 1f;

        Vector3 target = origin + offset;
        NavMeshHit hit;
        if(NavMesh.SamplePosition(target, out hit, 2.5f, NavMesh.AllAreas)){
            agent.SetDestination(hit.position);
        }
    }

    void SearchPair(Vector3 center){
        int side = (++sInvestigateCounter & 1) == 0 ? 1 : -1;
        Vector3 toGuard = (transform.position - center);
        toGuard.y = 0;
        if(toGuard.sqrMagnitude < 0.01f) toGuard = Random.insideUnitSphere;
        toGuard.y = 0;
        Vector3 right = Vector3.Cross(Vector3.up, toGuard.normalized);
        Vector3 p = center + right * (3f * side);

        NavMeshHit hit;
        if(NavMesh.SamplePosition(p, out hit, 3f, NavMesh.AllAreas)){
            agent.SetDestination(hit.position);
        }
    }

    bool EnsureOnMesh(float maxSnapDistance = 2f){
        if(!agent || !agent.enabled) return false;
        if(agent.isOnNavMesh) return true;

        if(NavMesh.SamplePosition(transform.position, out var hit, maxSnapDistance, NavMesh.AllAreas)){
            agent.Warp(hit.position);
            return true;
        }
        return false;
    }

}