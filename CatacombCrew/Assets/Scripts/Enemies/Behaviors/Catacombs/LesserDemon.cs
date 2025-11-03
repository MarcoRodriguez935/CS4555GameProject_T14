using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class LesserDemon : EnemyBase
{

    /*  The Lesser Demon acts as a guardian of the catacombs, monitoring the main area/exit, moving with the cultists and such
        he is blind, sensitive to sounds (low magnitude rays are heard, large 'listening' collider)
        teleports halfway to the player upon hearing a sound, listens for another; if another is heard and there is a path to it, he will charge to it
    */
    float proximityMagnitude = 8f; //fake magnitude inside of the collider
    float proximityCooldown = 0.15f;
    float nextProximityTime = 0f;

    private float walkSpeed = 2.5f;
    private float chargeSpeed = 10f; 
    private float investigateFor = 15f; //time spent patrolling a room sent to investigate in

    private float slamRadius = 5f; //radius of the slam attack performed at the end of a charge
    private bool refreshPath = false;

    public Transform[] patrolPoints;
    private int patrolDest = 0;
    private int currentDest = -1;

    private bool charging; 
    private bool investigating;
    private bool escorting;

    //needs to prioritize sounds that it hears so it focuses on just one
    private float focusedPriority;
    private Vector3 focusedSoundPos;
    private GameObject playerLock;

    //preventing listening ray spam due to large collider
    float listeningCooldown = 0.15f;
    float muteTime = 0f;


    public override void Awake(){
        if(!ears) ears = GetComponentInChildren<SphereCollider>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;
        agent.avoidancePriority = 75;
        agent.autoBraking = true;
        agent.stoppingDistance = 0.5f;
        stunned = false;

        ToNextRoom();
    }

    public override void Update(){
        //no base.Update as they are blind;
        if(escorting || charging || stunned || agent == null) return;

        if(!investigating && !charging && !agent.pathPending && agent.remainingDistance < 0.5f && !agent.isStopped){
            Debug.Log("Normal Patrol");
            ToNextRoom();
        } 
    }

    void OnTriggerStay(Collider other){
        if(escorting || stunned || agent == null) return;
        if(Time.time < nextProximityTime) return;

        if(!other.CompareTag("Player")) return;
        float distance = Vector3.Distance(transform.position, other.transform.position);
        if(distance > 15f) return;

        if(Physics.Linecast(transform.position, other.transform.position, sightMask, QueryTriggerInteraction.Ignore))
            return;

        nextProximityTime = Time.time + proximityCooldown;
        Vector3 origin = other.transform.position;
        Vector3 direction = (transform.position - origin).normalized;
        OnSound(origin, direction, proximityMagnitude, other.gameObject);
    }

    public override void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
        Debug.Log("Heard Something");

        if(escorting) return;

        if(Time.time < muteTime) return;
        muteTime = Time.time + listeningCooldown;

        Vector3 soundPos = origin;
        if(soundPos == Vector3.zero){
            if(reason != null) soundPos = reason.transform.position;
        }

        float distance = Vector3.Distance(soundPos, transform.position);
        float priority = magnitude / Mathf.Max(1f, distance);

        //going to be hearing a lot of sounds, focus on the loudest one instead of getting stuck on just one
        focusedPriority *= 0.85f;
        if(priority > focusedPriority){
            Debug.Log("New Priority");

            focusedPriority = priority; 
            focusedSoundPos = soundPos;
        }

        StartCoroutine(reactToSound(magnitude));
        heardPlayer = true;
        playerLock = reason;
        agent.ResetPath();
        agent.isStopped = true;

        if(!investigating){ //if investigating, teleport halfway to the sound source and patrol
            Debug.Log("Warping");
            Vector3 halfwayPoint = Vector3.Lerp(transform.position, soundPos, 0.5f);
            agent.Warp(halfwayPoint);

            agent.speed = walkSpeed;
            focusedSoundPos = soundPos;
            refreshPath = true;
            StartCoroutine(Investigate());
        }
        else{
            //if the player makes another noise close by during investigation; charge/slam
            NavMeshPath path = new NavMeshPath();
            bool hasPath = NavMesh.CalculatePath(transform.position, soundPos, NavMesh.AllAreas, path) 
                            && path.status == NavMeshPathStatus.PathComplete;


            if(hasPath){
                Debug.Log("Detected Close, Charging");
                focusedSoundPos = soundPos;
                StartCoroutine(ChargeAndSlam(focusedSoundPos));
            }
            else{
                focusedSoundPos = soundPos;
                refreshPath = true;
                // ClearRoom(origin);
            }
        }
        Debug.Log($"OnSound pos={soundPos} dist={distance:F1} p={priority:F2} focus={focusedPriority:F2} inv={investigating} charging={charging}");

    }

    IEnumerator ChargeAndSlam(Vector3 target){
        if(charging) yield break;
        charging = true;

        //charge
        agent.isStopped = false;
        agent.ResetPath();
        
        NavMeshHit hit;
        if(NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
            target = hit.position;
        
        agent.speed = chargeSpeed;

        if(!agent.SetDestination(target)){
            Debug.Log("Charge Failed To set Destination");
            charging = false;
            yield break;
        }

        while(agent.pathPending) yield return null;


        float timeout = Time.time + 3.5f;
        while(agent.remainingDistance > 0.6f && Time.time < timeout && !stunned){
            yield return null;
        }

        Debug.Log("Demon Slammed!");
        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius, sightMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits){
            //player takes damage if they are inside of the charge's slam radius at the end
        }

        yield return new WaitForSeconds(1.5f); //cooldown
        agent.ResetPath();
        agent.isStopped = true;
        agent.speed = walkSpeed;
        charging = false;

        focusedSoundPos = transform.position;
        refreshPath = true;
        if(!investigating) StartCoroutine(Investigate());
    }

    IEnumerator Investigate(){
        if(investigating) yield break;
        Debug.Log("Investigating");
        investigating = true;
        StartCoroutine(InvestigateTimer());

        heardPlayer = false;
        Queue<Vector3> roomPoints = new Queue<Vector3>();
        roomPoints.Enqueue(focusedSoundPos);

        Queue<Vector3> sweep = GetRoomPatrols(focusedSoundPos);
        foreach (var p in sweep) roomPoints.Enqueue(p);

        while(investigating && !charging && !stunned){
            if(refreshPath){
                roomPoints.Clear();
                roomPoints.Enqueue(focusedSoundPos);
                sweep = GetRoomPatrols(focusedSoundPos);
                foreach (var p in sweep) roomPoints.Enqueue(p);
                refreshPath = false;
            }
             
            if(roomPoints.Count == 0) break;

            Vector3 next = roomPoints.Dequeue();
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(next);

            while((agent.pathPending || agent.remainingDistance > 0.5f) && !charging && !stunned){
                yield return null;
            }
            yield return null;
        }

        investigating = false;
        focusedPriority = 0f;
        focusedSoundPos = transform.position;
    }

    IEnumerator InvestigateTimer(){
        investigating  = true;
        yield return new WaitForSeconds(investigateFor);
        investigating = false;
    }

    void ClearRoom(Vector3 focus){
        Debug.Log("Clearing Room");
        if(!investigating) StartCoroutine(Investigate());
        focusedSoundPos = focus;
        focusedPriority = Mathf.Max(focusedPriority, 0.1f);
    }

    void ToNextRoom(){ //patrolling behavior
        Debug.Log("Normal Patrol");
        if(patrolPoints.Length == 0)
            return;

        agent.speed = walkSpeed;
        agent.isStopped = false;

        currentDest = patrolDest;

        agent.destination = patrolPoints[currentDest].position;
        patrolDest = UnityEngine.Random.Range(0, patrolPoints.Length);

        if(agent.remainingDistance < 0.5f){
            heardPlayer = false;
            agent.speed = walkSpeed;
        }
    }

    Queue<Vector3> GetRoomPatrols(Vector3 around){
        Queue<Vector3> queue = new Queue<Vector3>();
        GameObject[] rooms = GameObject.FindGameObjectsWithTag("Room");
        GameObject nearestRoom = null;
        float best = float.PositiveInfinity;
        foreach (var room in rooms){
            float d = (room.transform.position - around).sqrMagnitude;
            if(d < best){
                best = d;
                nearestRoom = room;
            }
        }

        if(nearestRoom != null){
            foreach(Transform child in nearestRoom.transform){
                queue.Enqueue(child.position);
            }
        }
        return queue;
    }
}