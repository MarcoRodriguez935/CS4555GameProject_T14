using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Watchtower : EnemyBase
{

    /*  The Lesser Demon acts as a guardian of the catacombs, monitoring the main area/exit, moving with the cultists and such
        he is blind, sensitive to sounds (low magnitude rays are heard, large 'listening' collider)
        teleports halfway to the player upon hearing a sound, listens for another; if another is heard and there is a path to it, he will charge to it
    */

    private float walkSpeed = 2f;
    private float chargeSpeed = 5f; 
    private float investigateFor = 15f; //time spent patrolling a room sent to investigate in

    private float detectRadius = 3f; //how close the players can be before they are detected without making noise
    private float slamRadius = 5f; //radius of the slam attack performed at the end of a charge

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
    float listeningCooldown = 0.5f;
    float muteTime = 0f;


    public override void Awake(){

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
            ToNextRoom();
        } 
    }

    public override void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
        if(escorting) return;

        if(Time.time < muteTime) return;
        muteTime = Time.time + listeningCooldown;


        float distance = Vector3.Distance(origin, transform.position);
        float priority = magnitude / Mathf.Max(1f, distance);

        //going to be hearing a lot of sounds, focus on the loudest one instead of getting stuck on just one
        if(priority > focusedPriority){
            focusedPriority = priority; 
            focusedSoundPos = origin;
        }

        StartCoroutine(reactToSound(magnitude));
        heardPlayer = true;
        playerLock = reason;

        if(!investigating){ //if investigating, teleport halfway to the sound source and patrol
            Vector3 halfwayPoint = Vector3.Lerp(transform.position, origin, 0.5f);
            agent.Warp(halfwayPoint);
            agent.speed = walkSpeed;
            focusedSoundPos = origin;
            StartCoroutine(Investigate());
        }
        else{ //if the player makes another noise close by during investigation; charge/slam
            if(distance <= detectRadius){
                focusedSoundPos = origin;
                StartCoroutine(ChargeAndSlam(focusedSoundPos));
            }
            else{
                ClearRoom(origin);
            }
        }
    }

    IEnumerator ChargeAndSlam(Vector3 target){
        if(charging) yield break;
        charging = true;

        //charge
        agent.isStopped = false;
        agent.speed = chargeSpeed;
        
        if(!agent.SetDestination(target)){
            charging = false;
            yield break;
        }

        while(agent.pathPending) yield return null;

        //when reaching the sound origin point, slam attack
        while(agent.remainingDistance > 0.6f && !stunned){
            yield return null;
        }

        Debug.Log("Demon Slammed!");
        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius, sightMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits){
            //player takes damage if they are inside of the charge's slam radius at the end
        }


        agent.speed = walkSpeed;
        charging = false;
        yield return new WaitForSeconds(0.5f); //cooldown

    }

    IEnumerator Investigate(){
        if(investigating) yield break;
        investigating = true;

        heardPlayer = false;
        Queue<Vector3> roomPoints = GetRoomPatrols(focusedSoundPos);
        StartCoroutine(InvestigateTimer());

        while(investigating && !charging && !stunned){
            if(roomPoints.Count == 0){
                roomPoints = GetRoomPatrols(focusedSoundPos);
                if(roomPoints.Count == 0) break;
            }
            Vector3 next = roomPoints.Dequeue();
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(next);

            while(!agent.pathPending && agent.remainingDistance > 0.5f && !charging && !stunned){
                yield return null;
            }
            yield return null;
        }

        investigating = false;
        focusedPriority = 0f;
        focusedSoundPos = transform.position;
        ToNextRoom();
    }

    IEnumerator InvestigateTimer(){
        investigating  = true;
        yield return new WaitForSeconds(investigateFor);
        investigating = false;
    }

    void ClearRoom(Vector3 focus){
        if(!investigating) StartCoroutine(Investigate());
        focusedSoundPos = focus;
        focusedPriority = Mathf.Max(focusedPriority, 0.1f);
    }

    void ToNextRoom(){ //patrolling behavior
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