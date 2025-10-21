using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public abstract class EnemyBase : MonoBehaviour, SoundHeard, PlayerSeen
{

    /* abstract class to handle basic enemy movement, processing stimuli, driving behavior
        every enemy has a transform, reactiontime, navMesh
        some enemies can be blind, stunned, patrolroutes, investigations or panics (bools)
    */

    public GameObject self;
    protected float reactionTime = .75f;
    protected NavMeshAgent agent;
    
    public Transform eyes;
    private Transform Eye => eyes;

    private LayerMask sightMask;
    private LayerMask obstructionMask;
    private Vector3 boxHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
    protected float sightDistance;
    protected Vector3 seenLocation;


    //sound ray captured
    protected Vector3 soundOrigin;
    protected float magnitude;
    protected GameObject source;

    //states
    protected bool heardPlayer;
    protected bool sawPlayer;
    protected bool blind;
    protected bool stunned;

    public virtual void Awake(){
        sightMask = LayerMask.GetMask("Player");
        obstructionMask = LayerMask.GetMask("Wall", "Obstacle");
    }

    public virtual void Update(){
        if(Physics.BoxCast(eyes.position, boxHalfExtents, eyes.forward, out var seen, eyes.rotation, sightDistance, sightMask, QueryTriggerInteraction.Ignore)){
            Debug.DrawRay(eyes.position, eyes.forward * sightDistance, Color.green, 1f);

            var collider = seen.collider;
            Vector3 target = collider.bounds.center;

            bool blocked = Physics.Linecast(eyes.position, target, obstructionMask, QueryTriggerInteraction.Ignore);
            Debug.DrawLine(eyes.position, target, blocked ? Color.yellow : Color.green, 0f, false);

            if(!blocked){
                sawPlayer= true;
                var playerBody = seen.collider.attachedRigidbody;
                StartCoroutine(reactToSight(seen.point));
                OnSeen(seen.point, playerBody);
            }
        }
    }

    //virtual keyword allows the methods to be overridden?
    public virtual void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
        float distance = Vector3.Distance(origin, transform.position);
        heardPlayer= true;
        StartCoroutine(reactToSound(magnitude));
        agent.SetDestination(origin);
    }
    public virtual void OnSeen(Vector3 origin, Rigidbody playerLocation){
        float distance = Vector3.Distance(origin, transform.position);
        seenLocation = origin;
    }
    public virtual IEnumerator reactToSound(float magnitude){
        // Debug.Log("I heardPlayersomething with magnitude: " + magnitude + " i will react in : " + reactionTime);
        yield return new WaitForSeconds(reactionTime);
    }
    public virtual IEnumerator reactToSight(Vector3 playerSeen){
        float distance = Vector3.Distance(playerSeen, transform.position);
        float delay = reactionTime * (distance * 0.65f);
        yield return new WaitForSeconds(delay);
    }
}