using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class GiantRats : EnemyBase
{

    /*  Giant Rats are found in the sewers; they go to nests and wander around the sewers periodically
        If the players get close to them while they are wandering, they will attack once before fleeing to the nest
        if they are seen/heard from afar, the rats will simply move to another point in the area that doesn't cross the player
        If the players get close to the nests, all of the rats present at/close to the nest will chase them for some time
            the players will be given hiding places (fahed trap setup/pressure plate) ; rats stop swarming after some time of silence
    */

    RatNest nest;
    Queue<Vector3> wanderPoints;

    private float walkSpeed = 2f;

    private float detectRadius = 3f; //range for players to be heard or seen for attack during wander
    private float slamRadius = 5f; //range for players to be within nests to be swarmed
    private float interestTimer = 10f; //seconds of silence/no LOS before the rats stop swarming

    private int wanderDest = 0;
    private int currentDest = -1;
    private Transform nestRoot;

    private bool wandering;
    private bool chasing; //
    private bool swarming;

    // public override void Awake(){

    //     agent = GetComponent<NavMeshAgent>();
    //     agent.speed = walkSpeed;
    //     agent.avoidancePriority = UnityEngine.Random.Range(30, 70);
    //     agent.autoBraking = true;
    //     agent.stoppingDistance = 0.5f;
    //     stunned = false;

    //     ReturnToNest();
    // }

    // public override void Update(){
    //     //no base.Update as they are blind;
    //     if(escorting || charging || stunned || agent == null) return;

    //     if(!investigating && !charging && !agent.pathPending && agent.remainingDistance < 0.5f && !agent.isStopped){
    //         ToNextRoom();
    //     } 
    // }

    // public override void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
    //     if(escorting) return;

    //     if(Time.time < muteTime) return;
    //     muteTime = Time.time + listeningCooldown;


    //     float distance = Vector3.Distance(origin, transform.position);
    //     float priority = magnitude / Mathf.Max(1f, distance);

    //     //going to be hearing a lot of sounds, focus on the loudest one instead of getting stuck on just one
    //     if(priority > focusedPriority){
    //         focusedPriority = priority; 
    //         focusedSoundPos = origin;
    //     }

    //     StartCoroutine(reactToSound(magnitude));
    //     heardPlayer = true;
    //     playerLock = reason;

    //     if(!investigating){ //if investigating, teleport halfway to the sound source and patrol
    //         Vector3 halfwayPoint = Vector3.Lerp(transform.position, origin, 0.5f);
    //         agent.Warp(halfwayPoint);
    //         agent.speed = walkSpeed;
    //         focusedSoundPos = origin;
    //         StartCoroutine(Investigate());
    //     }
    //     else{ //if the player makes another noise close by during investigation; charge/slam
    //         if(distance <= detectRadius){
    //             focusedSoundPos = origin;
    //             StartCoroutine(ChargeAndSlam(focusedSoundPos));
    //         }
    //         else{
    //             ClearRoom(origin);
    //         }
    //     }
    // }

    public void AssignWander(Queue<Vector3> route){
        wanderPoints = route;
    }

    void WanderToPoint(){ //chooses a random subset of points from total in sewers and queues them
        if(sawPlayer || heardPlayer) return;

        if(wanderPoints.Length == 0)
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

    IEnumerator Investigate(){
        while(investigating && !stunned){
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
    }

    public void Burrow(float seconds){
        //if the rats end their wandering route and return
        //they will burrow for a random amount of time before getting new route
    }
    public void Swarm(Vector3 target){
        //if players inside of nest's range and a rat sees them,
        //all the rats inside the nest and around will swarm for 20s
    }
}