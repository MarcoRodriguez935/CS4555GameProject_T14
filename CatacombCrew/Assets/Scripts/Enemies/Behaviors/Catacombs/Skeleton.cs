using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Skeleton : EnemyBase
{
    Cultist spawner;
    private float skeletonLifetime = 75f;

    private float chaseSpeed = 2.85f;
    private float patrolSpeed = 2.5f;
    private Vector3 chaseOffset;
    private Transform trackedPlayer;
    private float feedUntil = -1f;

    private float searchRadius = 12f;

    private float loseSightTime = 2f;
    private float investigateTime = 3f;

    public Transform[] patrolPoints;
    private int patrolDest;
    private int autoPatrol = 5;

    private SkeletonAnimation skeletonAnimation;
    private EnemyAttack enemyAttack;

    private float spawnLift = 0.5f;

    private Vector3 spawnP;
    private Vector3 lastSeenLocation;
    private Vector3 lastHeardLocation;
    private float lastSeenTime = -1f;
    private float lastHeardTime = -1f;

    private bool investigating;
    private bool chasing;


    public override void Awake(){
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        enemyAttack = GetComponent<EnemyAttack>();
        base.Awake();
        sightDistance = 8f;
        agent = GetComponent<NavMeshAgent>();
        agent.avoidancePriority = UnityEngine.Random.Range(30, 70);
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 1f;
        agent.isStopped = false;

        Vector3 probe = transform.position + Vector3.up * spawnLift;
        if(!NavMesh.SamplePosition(probe, out var hit, 3f, NavMesh.AllAreas)){
            Debug.Log($"{name}: No NavMesh near spawn ({probe}).");
        }
        else{
            agent.Warp(hit.position);
        }

        Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.6f;
        chaseOffset = new Vector3(offset.x, 0f, offset.y);

        spawnP = transform.position;
        GetPatrolRoute();

        var player = getClosestPlayer();
        if(player != null){
            Track(player, 3f);
        }

        StartCoroutine(Lifetime());
    }    

    public void Track(Transform target, float time){
        if(target == null) return;
        trackedPlayer = target;
        feedUntil = Mathf.Max(feedUntil, Time.time + time);
        sawPlayer = true;
        lastSeenTime = Time.time;
        lastSeenLocation = target.position;
        if(!chasing) StartCoroutine(Chase());
    }

    public void Init(Cultist owner){
        spawner = owner;
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        
        bool recentSeen = (Time.time - lastSeenTime) <= loseSightTime;
        if(!recentSeen) sawPlayer = false;

        if(!chasing && !investigating){
            if(!agent.pathPending && agent.remainingDistance < agent.stoppingDistance + 0.05f){
                NextPoint();
            } 
        }

        if(!chasing && recentSeen) StartCoroutine(Chase());
        if(heardPlayer && !chasing) StartCoroutine(Investigate(lastHeardLocation));

    }

    IEnumerator Investigate(Vector3 lastLocation){
        if(investigating) yield break;
        investigating = true;
        chasing = false;
        Debug.Log("We Heard You");


        //short reactiontime
        yield return new WaitForSeconds(.5f);

        agent.speed = patrolSpeed;
        agent.SetDestination(lastLocation);

        float checkTime = 0f;
        while(checkTime < investigateTime){
            if(sawPlayer && Time.time - lastSeenTime <= loseSightTime){
                investigating = false;
                StartCoroutine(Chase());
                yield break;
            }
            if(!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f){
                checkTime += Time.deltaTime;
            }
            else{
                checkTime = Mathf.Min(checkTime, investigateTime * 0.5f);
            }
            yield return null;
        }
        investigating = false;
        heardPlayer = false;
    }

    IEnumerator Chase(){
        if(chasing) yield break;
        chasing = true;
        investigating = false;
        Debug.Log("We Seen You");

        //short reactiontime
        yield return new WaitForSeconds(0.3f);

        agent.speed = chaseSpeed;

        while(true){
            bool feed = (trackedPlayer != null && Time.time < feedUntil);
            bool inLOS = (Time.time - lastSeenTime) <= loseSightTime;

            if(!feed && !inLOS){
                chasing = false;
                StartCoroutine(Investigate(lastSeenLocation));
                yield break;
            }

            Vector3 dest = feed ? trackedPlayer.position : lastSeenLocation;
            if(feed){
                lastSeenLocation = dest;
                lastSeenTime = Time.time;
            }

            Vector3 target = dest + chaseOffset;
            agent.SetDestination(target);

            yield return null;

        }
    }

    public override void OnSeen(Vector3 origin, Rigidbody playerLocation){
        sawPlayer = true;
        lastSeenTime = Time.time;
        lastSeenLocation = origin;
        
        if(playerLocation != null){
            trackedPlayer = playerLocation.transform;
        }
        else{
            trackedPlayer = getClosestPlayerTo(origin);
        }

        feedUntil = Time.time + loseSightTime;
        StartCoroutine(reactToSight(origin));

    }

     public override void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
        float distance = Vector3.Distance(origin, transform.position);

        if(magnitude >= 3.5f && distance <= searchRadius){
            heardPlayer = true;
            lastHeardTime = Time.time;
            lastHeardLocation = origin;
            StartCoroutine(reactToSound(magnitude));
        }
    }

    void GetPatrolRoute(){
        List<Transform> route = new List<Transform>();
        spawnP = transform.position;

        GameObject[] rooms = GameObject.FindGameObjectsWithTag("Room");
        GameObject nearest = null;
        float best = float.PositiveInfinity;
        foreach (var room in rooms){
            float distance = (room.transform.position - spawnP).sqrMagnitude;
            if(distance < best){
                best = distance;
                nearest = room;
            }
        }

        if(nearest != null){
            foreach(var child in nearest.GetComponentsInChildren<Transform>(true)){
                if(child.CompareTag("PatrolPoint")){
                    route.Add(child);
                }
            }
        }

        if(route.Count == 0){
            var points = GameObject.FindGameObjectsWithTag("PatrolPoint");
            foreach(var point in points){
                if((point.transform.position - spawnP).sqrMagnitude <= searchRadius * searchRadius){
                    route.Add(point.transform);
                }
            }
        }

        if(route.Count == 0){
            for(int i = 0; i < autoPatrol; i++){
                Vector3 cand = spawnP + Random.insideUnitSphere * (searchRadius * 0.6f);
                cand.y = spawnP.y + 0.5f;
                if(NavMesh.SamplePosition(cand, out var hit, 2.5f, NavMesh.AllAreas)){
                    var ghost = new GameObject($"AutoPatrol_{i}");
                    ghost.transform.position = hit.position;
                    route.Add(ghost.transform);
                }
            }
        }

        patrolPoints = route.ToArray();

        if(patrolPoints.Length > 0){
            float min = float.PositiveInfinity;
            int bestInd = 0;
            for(int i = 0; i < patrolPoints.Length; i++){
                float distance = (patrolPoints[i].position - spawnP).sqrMagnitude;
                if(distance < min){
                    min = distance;
                    bestInd = i;
                }
            }

            patrolDest = bestInd;

        }
    }

    void NextPoint(){
        if(patrolPoints.Length == 0)
            return;

        agent.destination = patrolPoints[patrolDest].position;
        // patrolDest = (patrolDest + 1) % patrolPoints.Length; //all to the same spot
        patrolDest = UnityEngine.Random.Range(0, patrolPoints.Length);

    }

    IEnumerator Lifetime(){
        yield return new WaitForSeconds(skeletonLifetime);
        Destroy(gameObject);
    }

    void OnDestroy(){
        spawner.OnSkeletonDeath(this);
    }

    private Transform getClosestPlayer(){
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closest = null;
        float bestSqr = float.PositiveInfinity;

        foreach(var p in players){
            float sqr = (p.transform.position - transform.position).sqrMagnitude;
            if(sqr < bestSqr){
                bestSqr = sqr;
                closest = p.transform;
            }
        }
        return closest;
    }

    private Transform getClosestPlayerTo(Vector3 point){
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closest = null;
        float bestSqr = float.PositiveInfinity;

        foreach(var p in players){
            float sqr = (p.transform.position - point).sqrMagnitude;
            if(sqr < bestSqr){
                bestSqr = sqr;
                closest = p.transform;
            }
        }
        return closest;
    }

}