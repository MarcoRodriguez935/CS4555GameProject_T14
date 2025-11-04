using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;


public class LizardMutants : EnemyBase
{
    // /*  LizardMutants patrol areas in the sewers; 
    // if they hear or see the player, they will begin to stalk them at a distance
    //     the players must scare them away by looking at them and moving at them
    //     if they fail to do this and 2 lizards are stalking at the same time, 
    //         they will attack the player all at once and chase for a short period of time
    // */

    public List<Transform> patrolPoints = new List<Transform>();
    private float patrolSpeed = 3f;
    private float stalkSpeed = 2.2f;
    private float huntSpeed = 4.2f;
    private float fleeSpeed = 3.8f;

    private float stalkRadius = 10f;
    private float stalkDistanceRadius = 9f;
    private float attackRange = 1.5f;
    private float fleeTime = 3f;
    private float resumePatrolTime = 10f;
    private float patrolUntil;
    private float nextRadiusCheck;

    private float huntLockTime = 5f;
    private float loseInterestAfter = 5f;

    static int stalkerCount = 0;
    private float stalkingMaxTime = 10f; //time players have to scare off one stalker if the max is reached before hunt

    enum LizardMode { Patrol, Stalk, Hunt, Flee, Attack }
    LizardMode state = LizardMode.Patrol;

    MutantLizardAnimation mutantLizardAnim;

    EnemyAttack enAttack;

    private float radiusCheckInterval = 0.75f;
    private float playerWalking = 3.5f;
    private float playerRunning = 5.5f;

    private int patrolDest;
    private float timeUntil;
    private float huntLockUntil;
    private float lastStimulusTime;
    private float stalkStart;

    private Transform playerLock;
    private Rigidbody playerBody;
    private float lastHeardMagnitude;
    private Vector3 lastKnownPos;

    static Dictionary<Transform, List<LizardMutants>> groups = new Dictionary<Transform,List<LizardMutants>>();

    public override void Awake(){
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.stoppingDistance = 0.65f;
        mutantLizardAnim = GetComponent<MutantLizardAnimation>();
        enAttack = GetComponent<EnemyAttack>();
        eyes = transform;
        ears = GetComponent<SphereCollider>();
        sightDistance = 15f;
    }

    public void Start(){
        agent.speed = patrolSpeed;

        patrolDest = 0;
        float bestD2 = float.PositiveInfinity;
        Vector3 pos = transform.position;
        for(int i = 0; i < patrolPoints.Count; i++){
            var t = patrolPoints[i];
            if(t == null) continue;
            float d2 = (t.position - pos).sqrMagnitude;
            if(d2 < bestD2){
                bestD2 = d2;
                patrolDest = i;
            }
        }

        agent.SetDestination(patrolPoints[patrolDest].position);
        state = LizardMode.Patrol;
    }

    public override void Update(){
        base.Update();
        switch(state){
            case LizardMode.Patrol:
                Patrol();
                if(Time.time >= nextRadiusCheck && Time.time >= patrolUntil){
                    nextRadiusCheck = Time.time + radiusCheckInterval;
                    CheckStalkRadius();
                }
                break;

            case LizardMode.Stalk:
                Debug.Log($"{gameObject} Stalking");
                StalkPlayer();
                break;

            case LizardMode.Hunt:
                Debug.Log("Hunting");
                Hunt();
                break;
            case LizardMode.Flee:
                Debug.Log("Fleeing");
                if(Time.time >= timeUntil){
                    state = LizardMode.Patrol;
                    playerLock = null;
                    playerBody = null;
                    agent.speed = patrolSpeed;
                    if(patrolPoints.Count > 0) agent.SetDestination(patrolPoints[patrolDest].position);
                }
                break;

            case LizardMode.Attack:
                Debug.Log("Attacking");
                if(Time.time >= timeUntil){
                    FleeFrom(lastKnownPos);
                }
                break;
        }
    }
    public void Patrol(){
        Debug.Log("Patrolling");
        //traverse through list pf points given to each lizard in inspector
        //if the player is seen very close, attack and flee
        //if the player is heard or inside of a stalking radius for short time, move to stalk
        agent.speed = patrolSpeed;

        if(patrolPoints.Count == 0) return;

        Vector3 target = patrolPoints[patrolDest].position;
        if((transform.position - target).sqrMagnitude <= 0.7f * 0.7f){
            patrolDest = (patrolDest + 1) % patrolPoints.Count;
            agent.SetDestination(patrolPoints[patrolDest].position);
        }

    }
    public void CheckStalkRadius(){
         //check stalking radius periodically, time players inside of it
        //if players inside long enough, begin to stalk
        int pLayer = LayerMask.NameToLayer("Player");
        if(pLayer < 0) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, stalkRadius, 1 << pLayer, QueryTriggerInteraction.Collide);
        Transform best = null;
        float bestD2 = float.MaxValue;
        Rigidbody body = null;

        for(int i = 0; i < hits.Length; i++){
            var rigid = hits[i].attachedRigidbody;
            var t = rigid ? rigid.transform : hits[i].transform;
            float d2 = (t.position - transform.position).sqrMagnitude;
            if(d2 < bestD2){
                bestD2 = d2; 
                best = t;
                body = rigid;
            }
        }

        if(best != null){
            playerLock = best;
            playerBody = body;
            lastKnownPos = playerLock.position;
            StalkPlayer();
        }

    }
    public void StalkPlayer(){
        //stalking should make the lizards follow the players at a distance
        //they should be fed the player's exact location while stalking
        //increment the stalkercount
        if(playerLock == null){
            LeaveGroup();
            state = LizardMode.Patrol;
            return;
        }
        if(state != LizardMode.Stalk){
            state = LizardMode.Stalk;
            agent.isStopped = false;
            agent.speed = stalkSpeed;
            stalkStart = Time.time;
            JoinGroup(playerLock, this);
            stalkerCount = GetStalkers();
        }
        
        lastKnownPos = playerLock.position;

        float distance = Vector3.Distance(transform.position, playerLock.position);
        if(distance > stalkRadius * 1.75f){
            LeaveGroup();
            playerLock = null;
            playerBody = null;
            state = LizardMode.Patrol;
            return;
        }

        int size;
        int index;
        GetGroupIndex(playerLock, this, out size, out index);
        size = Mathf.Max(1, size);
        float ring = Mathf.Max(1f, stalkDistanceRadius);
        float angle = (index / (float)size) * Mathf.PI * 2f;
        Vector3 ringOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
        Vector3 ringTarget = playerLock.position + ringOffset;

        NavMeshHit hit;
        if(NavMesh.SamplePosition(ringTarget, out hit, 2f, NavMesh.AllAreas)){
            ringTarget = hit.position;
        }
        else{
            bool found = false;
            for(int k = 1; k <= 3; k++){
                float a = angle + k * 0.35f;
                Vector3 off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ring;
                if(NavMesh.SamplePosition(playerLock.position + off, out hit, 2.5f, NavMesh.AllAreas)){
                    ringTarget = hit.position;
                    found = true;
                    break;
                }
                a = angle - k * 0.35f;
                off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ring;
                if(NavMesh.SamplePosition(playerLock.position + off, out hit, 2.5f, NavMesh.AllAreas)){
                    ringTarget = hit.position;
                    found = true;
                    break;
                }
            }
            if(!found) ringTarget = transform.position;
        }

        agent.SetDestination(ringTarget);

        Vector3 playerTowards = (transform.position - playerLock.position).normalized;
        float playerSpeed = (playerBody != null) ? playerBody.linearVelocity.magnitude : 0f;
        float towardDot = 0f;
        if(playerBody != null && playerBody.linearVelocity.sqrMagnitude > 0.01f){
            towardDot = Vector3.Dot(playerBody.linearVelocity.normalized, playerTowards);
        }

        if(playerSpeed > playerRunning && towardDot > 0.5f){
            ScaredOff();
            return;
        }

        bool playerApproaching = towardDot > 0.35f;

        if(distance <= attackRange && playerSpeed <= playerWalking && playerApproaching){
            AttackAndFlee();
            return;
        }

        if(GroupSize(playerLock) >= 2){
            float ignoredFor = Time.time - stalkStart;
            if(ignoredFor >= stalkingMaxTime){
                TriggerGroupHunt(playerLock);
            }
        }
    }

    public void AttackAndFlee(){
        //short attack on player before running to next patrol point
        state = LizardMode.Attack;
        agent.isStopped = true;
        timeUntil = Time.time + 0.75f;

        //attack animation
        mutantLizardAnim.AttackAnim();
        //damage calculation
        enAttack.Attack();

        if(playerLock != null) lastKnownPos = playerLock.position;
    }

    public void ScaredOff(){
        //if the player runs at the lizard, the lizard should flee and lose the lockon
        patrolUntil = Time.time + resumePatrolTime;
        FleeFrom(playerLock ? playerLock.position : transform.position); 
    }

    public void FleeFrom(Vector3 playerPosition){
        LeaveGroup();
        playerLock = null;
        playerBody = null;

        state = LizardMode.Flee;
        agent.isStopped = false;
        agent.speed = fleeSpeed;

        Vector3 fromPlayer = (transform.position - playerPosition).normalized;

        if(fromPlayer.sqrMagnitude < 4f){
            Vector3 side = Vector3.Cross(Vector3.up, fromPlayer).normalized;
            fromPlayer = (fromPlayer * 0.6f + side * (Random.value < 0.5f ? 0.8f : -0.8f)).normalized;
        }

        float fleeDist = Mathf.Max(6f, stalkRadius * 0.75f);
        Vector3 desired = transform.position + fromPlayer * fleeDist;

        NavMeshHit hit;
        if(!NavMesh.SamplePosition(desired, out hit, 3.0f, NavMesh.AllAreas)){
            bool found = false;
            for(int k = 1; k <= 3 && !found; k++){
                float delta = 0.35f * k;
                foreach(float s in new float[]{1f,-1f}){
                    Vector3 candDir = Quaternion.Euler(0f, delta * Mathf.Rad2Deg * s, 0f) * fromPlayer;
                    Vector3 cand = transform.position + candDir * fleeDist;
                    if(NavMesh.SamplePosition(cand, out hit, 3.5f, NavMesh.AllAreas)){
                        found = true;
                        break;
                    }
                }
            }
            if(!found) hit.position = transform.position;
        }

        agent.SetDestination(hit.position);
        timeUntil = Time.time + fleeTime;

    }

    public void CheckNoise(){
        //if the lizards are hunting and lose sight but hear player, they should move towards it and check a small area
        if(state == LizardMode.Hunt){
            Vector3 investigate = seenLocation;
            float area = 2f;
            investigate += new Vector3(Random.Range(-area, area), 0f, Random.Range(-area, area));
            agent.SetDestination(investigate);
        }
    }
    public void Hunt(){
        //all stalking lizards should have this triggered if the number is high enough and time runs out
        //the lizards will be fed the player's exact location for 6 seconds, refreshing if they are seen again
        //lose interest after 5 seconds of no stimuli (hearing or seeing)
        //if lose interest, go back to patrol for 15 seconds before starting another stalk
        agent.speed = huntSpeed;

        if(playerLock != null && Time.time <= huntLockUntil){
            lastKnownPos = playerLock.position;
        }

        agent.SetDestination(lastKnownPos);

        if(Time.time >= lastStimulusTime){
            LeaveGroup();
            playerLock = null;
            playerBody = null;
            state = LizardMode.Patrol;
            agent.speed = patrolSpeed;
            timeUntil = Time.time + resumePatrolTime;
            if(patrolPoints.Count > 0) agent.SetDestination(patrolPoints[patrolDest].position);
        }

    }

    public override void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
        base.OnSound(origin, currentDir, magnitude, reason);
        seenLocation = origin;

        if(state == LizardMode.Hunt){
            lastStimulusTime = Time.time + loseInterestAfter;
            lastKnownPos = origin;
            CheckNoise();
            return;
        }

        if(state == LizardMode.Patrol && Time.time >= patrolUntil){
            if(Vector3.Distance(transform.position, origin) <= stalkRadius * 1.15f){
                FindNearestPlayer(origin);
                if(playerLock != null) StalkPlayer();
            }
        }
    }

    public override void OnSeen(Vector3 origin, Rigidbody playerLocation){
        base.OnSeen(origin, playerLocation);
        lastKnownPos = origin;

        if(state == LizardMode.Hunt){
            lastStimulusTime = Time.time + loseInterestAfter;
            huntLockUntil = Time.time + huntLockTime;
        }

        if(playerLocation != null){
            playerLock = playerLocation.transform;
            playerBody = playerLocation;
        }

        if(state == LizardMode.Patrol && Time.time >= patrolUntil){
            float distance = Vector3.Distance(transform.position, origin);
            float speed = (playerBody != null) ? playerBody.linearVelocity.magnitude : 0f;
            if(distance <= attackRange && speed <= playerWalking){
                AttackAndFlee();
                return;
            }

            if(distance <= stalkRadius){
                StalkPlayer();
            }
        }
    }

    void FindNearestPlayer(Vector3 point){
        int pLayer = LayerMask.NameToLayer("Player");
        if(pLayer < 0) return;
        Collider[] hits = Physics.OverlapSphere(point, stalkRadius, 1 << pLayer, QueryTriggerInteraction.Collide);

        Transform best = null;
        float bestD2 = float.MaxValue;
        Rigidbody rigid = null;
        for(int i = 0; i < hits.Length; i++){
            var body = hits[i].attachedRigidbody;
            var t = body ? body.transform : hits[i].transform;
            float d2 = (t.position - point).sqrMagnitude;
            if(d2 < bestD2){
                bestD2 = d2;
                best = t;
                rigid = body;
            }
        }
        playerLock = best;
        playerBody = rigid;
    }

    static void JoinGroup(Transform player, LizardMutants lizard = null){
        if(player == null) return;
        if(!groups.TryGetValue(player, out var list)){
            list = new List<LizardMutants>(4);
            groups[player] = list;
        }
        if(lizard != null && !list.Contains(lizard)) list.Add(lizard);
    }

    void LeaveGroup(){
        if(playerLock == null) return;
        if(groups.TryGetValue(playerLock, out var list)){
            list.Remove(this);
            if(list.Count == 0) groups.Remove(playerLock);
        }

        stalkerCount = GetStalkers();
    }

    static int GroupSize(Transform player){
        if(player == null) return 0;
        if(!groups.TryGetValue(player, out var list)) return 0;
        return list.Count;
    }

    static void GetGroupIndex(Transform player, LizardMutants self,  out int size, out int index){
        size = 1;
        index = 0;
        if(player == null) return;
        if(!groups.TryGetValue(player, out var list)) return;
        size = list.Count;
        index = Mathf.Max(0, list.IndexOf(self));
    }

    static void TriggerGroupHunt(Transform player){
        if(player == null) return;
        if(!groups.TryGetValue(player, out var list) || list.Count < 2) return;

        for(int i = 0; i < list.Count; i++){
            var lizard = list[i];
            lizard.state = LizardMode.Hunt;
            lizard.agent.isStopped = false;
            lizard.agent.speed = lizard.huntSpeed;
            lizard.huntLockUntil = Time.time + lizard.huntLockTime;
            lizard.lastStimulusTime = Time.time + lizard.loseInterestAfter;
            lizard.lastKnownPos = player.position;
        }

    }   

    static int GetStalkers(){
        int total = 0;
        foreach(var locked in groups) total += locked.Value.Count;
        return total;
    }

}