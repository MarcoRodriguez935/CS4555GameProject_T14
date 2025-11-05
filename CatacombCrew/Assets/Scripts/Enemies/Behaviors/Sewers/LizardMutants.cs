using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;


public class LizardMutants : EnemyBase
{
    /*  LizardMutants patrol areas in the sewers; 
    if they hear or see the player, they will begin to stalk them at a distance
        the players must scare them away by looking at them and moving at them
        if they fail to do this and 2 lizards are stalking at the same time, 
            they will attack the player all at once and chase for a short period of time
    */

    public List<Transform> patrolPoints = new List<Transform>();
    private float patrolSpeed = 3.2f;
    private float stalkSpeed = 3.8f;
    private float huntSpeed = 4.2f;
    private float fleeSpeed = 10f;

    private float stalkRadius = 16f;
    private float stalkDistanceRadius = 12f;
    private float attackRange = 1.25f;
    private float fleeTime = 3f;
    private float resumePatrolTime = 10f;
    private float patrolUntil;
    private float nextRadiusCheck;

    private float huntLockTime = 5f;
    private float loseInterestAfter = 5f;

    private float sideSign = 0f;
    private float stalkingMaxTime = 10f; //time players have to scare off one stalker if the max is reached before hunt

    enum LizardMode { Patrol, Stalk, Hunt, Flee, Attack }
    LizardMode state = LizardMode.Patrol;

    MutantLizardAnimation mutantLizardAnim;

    EnemyAttack enAttack;

    private float radiusCheckInterval = 0.75f;
    private float playerWalking = 2.5f;
    private float playerRunning = 6f;

    private int patrolDest;
    private float timeUntil;
    private float huntLockUntil;
    private float lastStimulusTime;

    private Transform playerLock;
    private Rigidbody playerBody;
    private Vector3 lastKnownPos;

    static Dictionary<Transform, List<LizardMutants>> groups = new Dictionary<Transform,List<LizardMutants>>();
    static Dictionary<Transform, float> groupOverlap = new Dictionary<Transform,float>();


    public override void Awake(){
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.autoRepath = true;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.stoppingDistance = .8f;
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
                    bool playerClose = TryGetNearestPlayer(transform.position, stalkRadius * 0.9f, out var pClose, out _);
                    bool hasLOS = false;
                    if(playerClose){
                        hasLOS = !Physics.Linecast(eyes.position + Vector3.up * 0.4f, pClose.position + Vector3.up * 0.9f, obstructionMask, QueryTriggerInteraction.Ignore);
                    }

                    if(!playerClose || !hasLOS){
                        state = LizardMode.Patrol;
                        playerLock = null;
                        playerBody = null;
                        agent.speed = patrolSpeed;
                        if(patrolPoints.Count > 0) agent.SetDestination(patrolPoints[patrolDest].position);
                    }
                    else{
                        FleeFrom(pClose.position);
                        timeUntil = Time.time + 0.75f;
                    }
                }
                else if(!agent.pathPending && agent.remainingDistance <= 0.05f && agent.desiredVelocity.sqrMagnitude < 0.05f){
                    var from = playerLock ? playerLock.position : lastKnownPos;
                    FleeFrom(from);
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

    bool TryGetNearestPlayer(Vector3 origin, float radius, out Transform player, out Rigidbody body){
        player = null;
        body = null;

        int pLayer = LayerMask.NameToLayer("Player");
        if(pLayer < 0) return false;

        Collider[] hits = Physics.OverlapSphere(origin, radius, 1 << pLayer, QueryTriggerInteraction.Collide);

        float bestD2 = float.MaxValue;
        for(int i = 0; i < hits.Length; i++){
            var rb = hits[i].attachedRigidbody;
            var t = rb ? rb.transform : hits[i].transform;
            float d2 = (t.position - origin).sqrMagnitude;
            if(d2 < bestD2){
                bestD2 = d2;
                player = t;
                body = rb;
            }
        }
        return player != null;
    }


    public void Patrol(){
        Debug.Log("Patrolling");
        //traverse through list pf points given to each lizard in inspector
        //if the player is seen very close, attack and flee
        //if the player is heard or inside of a stalking radius for short time, move to stalk
        agent.speed = patrolSpeed;

        if(patrolPoints.Count == 0) return;

        Vector3 target = patrolPoints[patrolDest].position;
        float arrivalDist = agent.stoppingDistance;
        if(!agent.pathPending && agent.remainingDistance <= arrivalDist){
            patrolDest = (patrolDest + 1) % patrolPoints.Count;
            agent.SetDestination(patrolPoints[patrolDest].position);
        }

    }
    public void CheckStalkRadius(){
         //check stalking radius periodically, time players inside of it
        //if players inside long enough, begin to stalk
        if(TryGetNearestPlayer(transform.position, stalkRadius, out var best, out var rb)){
            playerLock = best;
            playerBody = rb;
            lastKnownPos = best.position;
            StalkPlayer();
        }

    }
    public void StalkPlayer(){
        //stalking should make the lizards follow the players at a distance
        //they should be fed the player's exact location while stalking
        //increment the stalkercount
        if(state != LizardMode.Stalk && Time.time < patrolUntil) return;

        if(playerLock == null){
            LeaveGroup();
            state = LizardMode.Patrol;
            return;
        }
        if(state != LizardMode.Stalk){
            state = LizardMode.Stalk;
            agent.isStopped = false;
            agent.speed = stalkSpeed;
            JoinGroup(playerLock, this);

            Vector3 fwdEnter = playerBody
            ? new Vector3(playerBody.linearVelocity.x, 0f, playerBody.linearVelocity.z)
            : new Vector3(playerLock.forward.x, 0f, playerLock.forward.z);
            if(fwdEnter.sqrMagnitude < 0.001f) fwdEnter = (transform.position - playerLock.position);
            fwdEnter.Normalize();

            Vector3 toMeEnter = transform.position - playerLock.position;
            toMeEnter.y = 0f;
            if(toMeEnter.sqrMagnitude < 0.001f) toMeEnter = -fwdEnter;
            toMeEnter.Normalize();

            sideSign = Mathf.Sign(Vector3.Cross(fwdEnter, toMeEnter).y);
            if(sideSign == 0f) sideSign = (Random.value < 0.5f) ? -1f : 1f;

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

        Vector3 fwd = (playerBody != null && playerBody.linearVelocity.sqrMagnitude > 0.25f)
        ? new Vector3(playerBody.linearVelocity.x, 0f, playerBody.linearVelocity.z)
        : new Vector3(playerLock.forward.x, 0f, playerLock.forward.z);
        if(fwd.sqrMagnitude < 0.0001f) fwd = (transform.position - playerLock.position);
        fwd.Normalize();

        Vector3 toMe = transform.position - playerLock.position; 
        toMe.y = 0f;
        if(toMe.sqrMagnitude < 0.0001f) toMe = -fwd;
        toMe.Normalize();

        int size;
        int index;
        GetGroupIndex(playerLock, this, out size, out index);
        size = Mathf.Max(1, size);
        float ring = Mathf.Max(1f, stalkDistanceRadius);

        Vector3 fwd2 = (playerBody != null && playerBody.linearVelocity.sqrMagnitude > 0.25f)
        ? new Vector3(playerBody.linearVelocity.x, 0f, playerBody.linearVelocity.z)
        : new Vector3(playerLock.forward.x, 0f, playerLock.forward.z);

        if(fwd2.sqrMagnitude < 0.0001f) fwd2 = (playerLock.position - transform.position);
        fwd2.Normalize();
        Vector3 right2 = Vector3.Cross(Vector3.up, fwd2).normalized;

        Vector3 toMe2 = transform.position - playerLock.position;
        toMe2.y = 0f;
        if(toMe2.sqrMagnitude < 0.0001f) toMe2 = -fwd2;
        toMe2.Normalize();

        float baseAng = Mathf.Atan2(Vector3.Dot(toMe2, right2), Vector3.Dot(toMe2, fwd2));
        float frontCone = 60f * Mathf.Deg2Rad;

        bool needNewRing = !agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete || agent.remainingDistance < 0.5f;
        if(needNewRing){
            Vector3 ringTarget = transform.position;
            bool foundRing = false;
            NavMeshPath path = new NavMeshPath();

            int[] order = new[]{3,-3,2,-2,1,-1,0};
            for(int i = 0; i < order.Length && !foundRing; i++){
                float delta = 0.5f * order[i];
                float candAng = baseAng + delta;

                if(Mathf.Sign(candAng) != Mathf.Sign(sideSign)) continue;
                if(Mathf.Abs(candAng) < frontCone) continue;

                Vector3 candDir = (fwd2 * Mathf.Cos(candAng) + right2 * Mathf.Sin(candAng)).normalized;
                Vector3 cand = playerLock.position + candDir * ring;

                if(NavMesh.SamplePosition(cand, out var hit, 2.5f, NavMesh.AllAreas) && agent.CalculatePath(hit.position, path)
                    && path.status == NavMeshPathStatus.PathComplete){
                    ringTarget = hit.position;
                    foundRing = true;
                }

            }

            if(foundRing){
                if(NavMesh.Raycast(agent.nextPosition, ringTarget, out var wall, agent.areaMask)){
                    Vector3 tangent = Vector3.Cross(Vector3.up, wall.normal).normalized * sideSign;
                    Vector3 wrap = wall.position + tangent * (agent.radius + 1f);
                    if(NavMesh.SamplePosition(wrap, out var wrapHit, 1.5f, agent.areaMask))
                        ringTarget = wrapHit.position;
                }

                if(NavMesh.FindClosestEdge(ringTarget, out var edge, agent.areaMask)){
                    ringTarget -= edge.normal * (agent.radius + 0.2f);
                    Vector3 tangent = Vector3.Cross(Vector3.up, edge.normal).normalized * sideSign;
                    ringTarget += tangent * (agent.radius + 0.4f);
                    if(NavMesh.SamplePosition(ringTarget, out var inner, 0.6f, agent.areaMask))
                        ringTarget = inner.position;
                }

                agent.SetDestination(ringTarget);
            }
            else{
                BackOffOnce(playerLock.position);
                return;
            }
        }

        Vector3 playerTowards = (transform.position - playerLock.position).normalized;
        float playerSpeed = (playerBody != null) ? playerBody.linearVelocity.magnitude : 0f;
        float towardDot = 0f;
        if(playerBody != null && playerBody.linearVelocity.sqrMagnitude > 0.01f){
            towardDot = Vector3.Dot(playerBody.linearVelocity.normalized, playerTowards);
        }

        bool playerApproaching = towardDot > 0.35f;
        float desired = stalkDistanceRadius;
        if(distance < desired * 0.85f && playerApproaching){
            BackOffOnce(playerLock.position);
            return;
        }

        bool isClosest = true;
        if(GroupSize(playerLock) > 1 && groups.TryGetValue(playerLock, out var list)){
            float myD2 = (transform.position - playerLock.position).sqrMagnitude;
            for(int i = 0; i < list.Count; i++){
                var other = list[i];
                if(other == this) continue;
                float d2 = (other.transform.position - playerLock.position).sqrMagnitude;
                if(d2 < myD2 - 0.01f){
                    isClosest = false;
                    break;
                }
            }
        }

        float lookDot = Vector3.Dot(playerLock.forward, (transform.position - playerLock.position).normalized);
        if(isClosest && distance < desired * 0.8f && playerSpeed > playerRunning && towardDot > 0.8f && lookDot > 0.6f){
            sideSign = 0f;
            ScaredOff();
            return;
        }


        bool clearLOS = !Physics.Linecast(eyes.position + Vector3.up * 0.4f, playerLock.position + Vector3.up * 0.9f, obstructionMask, QueryTriggerInteraction.Ignore);
        if(clearLOS && distance <= attackRange * 0.8f && playerSpeed <= playerWalking && playerApproaching){
            sideSign = 0f;
            AttackAndFlee();
            return;
        }

        int gsize = GroupSize(playerLock);
        if(gsize >= 2){
            if(!groupOverlap.ContainsKey(playerLock)){
                groupOverlap[playerLock] = Time.time;
            }
            else{
                float overlapFor = Time.time - groupOverlap[playerLock];
                if(overlapFor >= stalkingMaxTime){
                    TriggerGroupHunt(playerLock);
                    groupOverlap[playerLock] = float.PositiveInfinity;
                }
            }
        }
        else{
            groupOverlap.Remove(playerLock);
        }

    }

    public void BackOffOnce(Vector3 from){
        agent.speed = fleeSpeed;

        Vector3 away = (transform.position - from);
        away.y = 0f;
        if(away.sqrMagnitude < 0.0001f) away = transform.forward;
        away.Normalize();

        float step = Mathf.Max(3f, stalkDistanceRadius * 0.6f);
        Vector3 want = transform.position + away * step;

        if(!TryReachable(want, out var pos)){
            Vector3 side = Vector3.Cross(Vector3.up, away);
            Vector3 l = (away * 0.8f + side * 0.6f).normalized * step + transform.position;
            Vector3 r = (away * 0.8f - side * 0.6f).normalized * step + transform.position;

            if(!TryReachable(l, out pos) && !TryReachable(r, out pos)){
                pos = transform.position + away * 1.5f;
            }
        }

        if(!NavMesh.SamplePosition(pos, out var phit, Mathf.Max(agent.radius + 0.5f, 1.0f), agent.areaMask))
            phit.position = transform.position;

        var safe = phit.position;
        if(NavMesh.FindClosestEdge(safe, out var edge2, agent.areaMask)){
            safe -= edge2.normal * (agent.radius + 0.1f);
            if(NavMesh.SamplePosition(safe, out var inner2, 0.5f, agent.areaMask))
                safe = inner2.position;
        }

        agent.isStopped = false;
        agent.SetDestination(safe);
    }

    bool TryReachable(Vector3 desired, out Vector3 pos){
        pos = desired;
        if(!NavMesh.SamplePosition(desired, out var hit, 2.5f, NavMesh.AllAreas))
            return false;

        NavMeshPath p = new NavMeshPath();
        if(!agent.CalculatePath(hit.position, p) || p.status != NavMeshPathStatus.PathComplete)
            return false;

        pos = hit.position;
        return true;
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
        patrolUntil = Time.time + resumePatrolTime;
        nextRadiusCheck = patrolUntil + 0.25f;

        LeaveGroup();
        lastKnownPos = playerPosition;
        playerLock = null;
        playerBody = null;

        state = LizardMode.Flee;
        agent.isStopped = false;
        agent.speed = fleeSpeed;

        Vector3 fromPlayer = (transform.position - playerPosition);
        fromPlayer.y = 0f;

        if(fromPlayer.sqrMagnitude < (4f * 4f)){
            Vector3 side = Vector3.Cross(Vector3.up, fromPlayer).normalized;
            fromPlayer = (fromPlayer.normalized * 0.6f + side * (Random.value < 0.5f ? 0.8f : -0.8f)).normalized;
        }
        else{
            fromPlayer.Normalize();
        }

        float fleeDist = Mathf.Max(6f, stalkRadius * 0.75f);
        Vector3 desired = transform.position + fromPlayer * fleeDist;

        if(NavMesh.Raycast(playerPosition, transform.position, out var hitRay, agent.areaMask)){
            Vector3 pushPast = hitRay.position + hitRay.normal * (agent.radius + 1.2f);
            Vector3 tangent = Vector3.Cross(Vector3.up, hitRay.normal).normalized * (Random.value < 0.5f ? 1f : -1f);
            Vector3 prefer = pushPast + tangent * 0.75f;
            if(NavMesh.SamplePosition(prefer, out var prefHit, 1.5f, agent.areaMask)){
                desired = prefHit.position;
            }
        }   

        NavMeshHit hit;
        float maxSnap = Mathf.Max(agent.radius + 0.5f, 1.0f);
        if(!NavMesh.SamplePosition(desired, out hit, maxSnap, agent.areaMask)){
            bool found = false;
            for(int k = 1; k <= 3 && !found; k++){
                float delta = 0.35f * k;
                foreach(float s in new float[]{1f,-1f}){
                    Vector3 candDir = Quaternion.Euler(0f, delta * Mathf.Rad2Deg * s, 0f) * fromPlayer;
                    Vector3 cand = transform.position + candDir * fleeDist;
                    if(NavMesh.SamplePosition(cand, out hit, maxSnap + 0.5f, agent.areaMask)){
                        found = true;
                        break;
                    }
                }
            }
            if(!found) hit.position = transform.position;
        }

        var target = hit.position;
        if(NavMesh.FindClosestEdge(target, out var edge, agent.areaMask)){
            target -= edge.normal * (agent.radius + 0.1f);
            if(NavMesh.SamplePosition(target, out var inner, 0.5f, agent.areaMask))
                target = inner.position;
        }

        agent.SetDestination(target);
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
                if(TryGetNearestPlayer(origin, stalkRadius, out var best, out var rb)){
                    playerLock = best;
                    playerBody = rb;
                    lastKnownPos = best.position;
                    StalkPlayer();
                }
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
            if(list.Count < 2){
                groupOverlap.Remove(playerLock);
            }
            if(list.Count == 0) groups.Remove(playerLock);
        }
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