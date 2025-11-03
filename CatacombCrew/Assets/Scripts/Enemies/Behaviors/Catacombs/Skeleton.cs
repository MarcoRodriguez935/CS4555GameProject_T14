using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Skeleton : EnemyBase
{
    Cultist spawner;
    private float skeletonLifetime = 30f;

    private float chaseSpeed = 3.2f;
    private float patrolSpeed = 2.8f;

    private float searchRadius = 12f;

    private Transform trackedPlayer;
    private float feedUntil = -1f;
    private float loseSightTime = 3f;
    private float investigateTime = 3f;
    private Vector3 chaseOffset;

    private Transform[] patrolPoints;
    private int patrolDest;

    private SkeletonAnimation skeletonAnimation;
    private EnemyAttack enemyAttack;

    private Vector3 spawnP;
    private Vector3 lastSeenLocation;
    private Vector3 lastHeardLocation;
    private float lastSeenTime = -1000f;
    private float lastHeardTime = -1000f;

    private bool investigating;
    private bool chasing;

    public override void Awake(){
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        enemyAttack = GetComponent<EnemyAttack>();
        base.Awake();

        sightDistance = 8f;
        agent = GetComponent<NavMeshAgent>();
        agent.avoidancePriority = UnityEngine.Random.Range(30, 70);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 1f;
        agent.isStopped = false;

        if(!agent.isOnNavMesh){
            if(NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas)){
                agent.Warp(hit.position);
            }
        }

        Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.6f;
        chaseOffset = new Vector3(offset.x, 0f, offset.y);

        spawnP = transform.position;
        GetPatrolRoute();

        var player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if(player != null){
            Track(player, 3f);
        }

        StartCoroutine(Lifetime());
    }    

    public void Init(Cultist owner){
        spawner = owner;
    }

    public override void Update(){
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

        //short reaction time
        yield return new WaitForSeconds(0.15f);

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
        lastSeenLocation = playerLocation != null ? playerLocation.position : origin;

        if(playerLocation != null){
            trackedPlayer = playerLocation.transform;
        }
        else if(trackedPlayer == null){
            var player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if(player) trackedPlayer = player;
        }

        feedUntil = Time.time + loseSightTime;
        StartCoroutine(reactToSight(origin));
    }

    public void Track(Transform target, float seconds){
        if(target == null) return;
        trackedPlayer = target;
        feedUntil = Mathf.Max(feedUntil, Time.time + seconds);

        sawPlayer = true;
        lastSeenTime = Time.time;
        lastSeenLocation = target.position;
        if(!chasing) StartCoroutine(Chase());    
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
        var points = GameObject.FindGameObjectsWithTag("PatrolPoint");
        var routePoints = new List<Transform>();
        foreach (var i in points){
            if((i.transform.position - spawnP).sqrMagnitude <= searchRadius * searchRadius){
                routePoints.Add(i.transform);
            }
        }
        patrolPoints = routePoints.ToArray();

        if(patrolPoints.Length > 0){
            float best  = float.MaxValue;
            int bestPoint = 0;
            for(int i = 0 ; i < patrolPoints.Length ; i++){
                float distance = Vector3.Distance(patrolPoints[i].position, spawnP);
                if(distance < best){
                    best = distance;
                    bestPoint = i;
                }
            }
            patrolDest = bestPoint;
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
}