using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;


public class UndeadGuards : EnemyBase
{
    /*
     * The Undead Guards are intelligent patrol enemies that patrol rooms (get points from a room object, patrol it for a while, then move to the next room)
     * if there are no rooms the guards will simply follow a regular patrol route assigned to them
     * If they see the players, they will give chase and attempt to intercept them using group tactics to surround them.
     * if they hear the players, they will pair up and investigate opposite sides of the sounds (left and right side of the source)
     * when one sees the players, he alerts his partner;
     * Guards can also be alerted by watchtowers (replace onsound with a chasing method inside watchtower), and called
     * upon by the king when he hears a sound.
     * When the players are nearby, the guards will poke at them with their spears and give chase
     * if the players are far and being chased, the guards should be fed their location until they lose los
     * when they lose los they should pair up and search the area (again on opposite sides)
     * when chasing, they should attempt to cut them off.
     */

    public List<Transform> roomList = new List<Transform>();
    public Queue<Transform> roomQueue = new Queue<Transform>();
    private Transform currentRoom;
    private List<Transform> currentRoomPoints = new List<Transform>();
    private int currentDest = -1;
    private bool loopRooms = true;

    private float patrolSpeed = 3.2f;
    private float chaseSpeed = 4.3f;
    private float pokeRange = 3f;
    private float loseInterestAfter = 6f;
    private float searchTime = 5f;

    enum GuardState { Patrol, Chase, Investigate, Search }
    GuardState state = GuardState.Patrol;

    private float interestUntil;
    private float searchUntil;

    private Transform playerLock;
    private Rigidbody playerBody;
    private Vector3 lastKnownPos;

    static int sInvestigateCounter = 0;
    private bool ready;

    public override void Awake(){
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.autoRepath = true;
        agent.autoBraking = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);
        agent.acceleration = Mathf.Max(agent.acceleration, 12f);
        agent.angularSpeed = Mathf.Max(agent.angularSpeed, 540f);
        agent.stoppingDistance = 0.25f;
        if(eyes == null) eyes = transform;
        sightDistance = Mathf.Max(sightDistance, 14f);

        seenLocation = transform.position;
        lastKnownPos = transform.position;
    }

    public void Start(){
        EnsureOnMesh(3f);
        agent.speed = patrolSpeed;
        agent.updateRotation = false;
        agent.updatePosition = false;
        agent.isStopped = true;
        agent.ResetPath();

        SeedQueueFromList(roomList);
        StartCoroutine(InitPatrol());
    }

    IEnumerator InitPatrol(){
        yield return null;

        float timeout = Time.time + 2f;
        while(Time.time < timeout){
            LoadNextRoom();
            if(currentRoom == null || currentRoomPoints.Count > 0){
                break;
            }
            yield return null;
        }

        ready = currentRoom != null && currentRoomPoints.Count > 0;

        if(ready){
            agent.nextPosition = transform.position;
            agent.Warp(transform.position);
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
            NextPatrolPoint();
        }
    }

    public override void Update(){
        if(!ready){
            if(agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
            agent.nextPosition = transform.position;
            return;
        }

        if(!EnsureOnMesh(3f)) return;
        base.Update();

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

        if(currentRoom != null && currentRoomPoints.Count == 0){
            MoveToNextRoom();
            return;
        }

        if(!agent.hasPath || agent.remainingDistance <= 0.4f){
            NextPatrolPoint();
        }
    }

    void NextPatrolPoint(){
        if(currentRoom == null){
            MoveToNextRoom();
            if(currentRoom == null){
                agent.ResetPath();
                return;
            }
        }

        if(currentRoomPoints.Count == 0){
            MoveToNextRoom();
            return;
        }

        currentDest++;

        if(currentDest >= currentRoomPoints.Count){
            MoveToNextRoom();
            return;
        }

        var t = currentRoomPoints[currentDest];
        if(!t){
            NextPatrolPoint();
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(t.position);
    }

    void MoveToNextRoom(){
        if(loopRooms && currentRoom) roomQueue.Enqueue(currentRoom);
        currentRoom = null;
        currentRoomPoints.Clear();
        currentDest = -1;
        LoadNextRoom();
        if(currentRoom != null) NextPatrolPoint();

    }

    void LoadNextRoom(){
        if(roomQueue.Count == 0) return;
        currentRoom = roomQueue.Dequeue();
        CollectRoomPoints(currentRoom, currentRoomPoints);
        currentDest = -1;
    }

    void CollectRoomPoints(Transform room, List<Transform> buffer){
        buffer.Clear();
        var children = room.GetComponentsInChildren<Transform>(true);
        for(int i = 0; i < children.Length; i++){
            var c = children[i];
            if(c && c.CompareTag("PatrolPoint")){
                buffer.Add(c);
            }
        }
    }

    public void SeedQueueFromList(List<Transform> rooms){
        if(rooms == null) return;
        var seen = new HashSet<Transform>();
        for(int i = 0; i < rooms.Count; i++){
            var room = rooms[i];
            if(room && seen.Add(room)) roomQueue.Enqueue(room);
        }
    }

    void Chase(){
        agent.speed = chaseSpeed;

        if(playerLock != null){
            var vel = (playerBody ? playerBody.linearVelocity : Vector3.zero);
            float dist = playerLock ? Vector3.Distance(transform.position, playerLock.position) : Vector3.Distance(transform.position, lastKnownPos);
            float leadTime = Mathf.Clamp(dist / Mathf.Max(agent.speed, 0.1f), 0.1f, 1.5f);
            lastKnownPos = playerLock.position + vel * leadTime;
            interestUntil = Time.time + loseInterestAfter;
        }

        float surroundRadius = 2.2f;
        Vector3 target;
        float distance = Vector3.Distance(transform.position, lastKnownPos);
        if(distance > surroundRadius * 1.5f){
            Vector3 toTarget = (lastKnownPos - transform.position);
            Vector3 side = toTarget.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, toTarget.normalized) : transform.right;
            int laneIndex = (Mathf.Abs(gameObject.GetInstanceID()) % 3) - 1;
            float laneWidth = 1.0f;
            target = lastKnownPos + side * (laneIndex * laneWidth);
        }
        else{
            int ringSlots = 4;
            int slot = Mathf.Abs(gameObject.GetInstanceID()) % ringSlots;
            float angle = slot * Mathf.PI * 2f / ringSlots;
            Vector3 ring = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * surroundRadius;
            target = lastKnownPos + ring;
        }

        agent.isStopped = false;
        agent.SetDestination(target);

        if(distance <= pokeRange){
            Debug.Log("Attacking");
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
            agent.isStopped = false;
            agent.SetDestination(point);
        }
    }

    public override void OnSeen(Vector3 origin, Rigidbody body){
        if(!ready) return;

        playerLock = body ? body.transform : playerLock;
        playerBody = body != null ? body : playerBody;
        lastKnownPos = origin;
        interestUntil = Time.time + loseInterestAfter;

        state = GuardState.Chase;
        agent.isStopped = false;
    }

    public override void OnSound(Vector3 origin, Vector3 direction, float magnitude, GameObject reason){
        if(!ready) return;
        base.OnSound(origin, direction, magnitude, reason);

        if(state == GuardState.Chase){
            lastKnownPos = origin;
            interestUntil = Time.time + loseInterestAfter;
            return;
        }

        if(reason && (reason.GetComponent<Watchtower>() != null)){
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

    void InvestigatePair(Vector3 origin){
        int side = (++sInvestigateCounter & 1) == 0 ? 1 : -1;
        Vector3 toGuard = (transform.position - origin);
        toGuard.y = 0f;
        Vector3 right = Vector3.Cross(Vector3.up, toGuard.normalized);
        Vector3 offset = right * (2f * side) + toGuard.normalized * 1f;

        Vector3 target = origin + offset;
        agent.isStopped = false;
        agent.SetDestination(target);
    }

    void SearchPair(Vector3 center){
        int side = (++sInvestigateCounter & 1) == 0 ? 1 : -1;
        Vector3 toGuard = (transform.position - center);
        toGuard.y = 0;
        if(toGuard.sqrMagnitude < 0.01f) toGuard = Random.insideUnitSphere;
        toGuard.y = 0;
        Vector3 right = Vector3.Cross(Vector3.up, toGuard.normalized);
        Vector3 p = center + right * (3f * side);

        agent.isStopped = false;
        agent.SetDestination(p);
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