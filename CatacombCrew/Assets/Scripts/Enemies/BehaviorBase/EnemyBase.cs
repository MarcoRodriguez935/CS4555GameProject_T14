using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public abstract class EnemyBase : MonoBehaviour, SoundHeard, PlayerSeen
{

    /* abstract class to handle basic enemy movement, processing stimuli, driving behavior
        every enemy has a transform, reactiontime, navMesh
        some enemies can be blind, stunned, have patrolroutes, investigations or panics (bools)
    */

    public GameObject self;
    protected float reactionTime = .15f;
    protected NavMeshAgent agent;
    
    public Transform eyes;
    private Transform Eye => eyes;

    public SphereCollider ears;
    private SphereCollider Ear => ears;

    protected LayerMask sightMask;
    protected LayerMask obstructionMask;
    private Vector3 boxHalfExtents = new Vector3(0.5f, 0.5f, 0.5f);
    protected float sightDistance;
    protected Vector3 seenLocation;
    private  float nextReactAt;

    //sound ray captured
    protected Vector3 soundOrigin;
    protected float magnitude;
    protected GameObject source;

    //states
    protected bool heardPlayer;
    protected bool sawPlayer;
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
                sawPlayer = true;

                if(Time.time >= nextReactAt){
                    nextReactAt = Time.time + reactionTime;
                    StartCoroutine(reactToSight(seen.point));

                }
                OnSeen(seen.point, seen.collider.attachedRigidbody);
            }
        }
    }

    //virtual keyword allows the methods to be overridden?
    public virtual void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
        heardPlayer = true;
        StartCoroutine(reactToSound(magnitude));
        seenLocation = origin;
    }
    public virtual void OnSeen(Vector3 origin, Rigidbody playerLocation){
        float distance = Vector3.Distance(origin, transform.position);
        StartCoroutine(reactToSight(origin));
        seenLocation = origin;
    }
    public virtual IEnumerator reactToSound(float magnitude){
        agent.isStopped = true;
        // Debug.Log("I heardPlayersomething with magnitude: " + magnitude + " i will react in : " + reactionTime);
        yield return new WaitForSeconds(reactionTime);
        agent.isStopped = false;
    }
    public virtual IEnumerator reactToSight(Vector3 playerSeen){
        agent.isStopped = true;
        yield return new WaitForSeconds(reactionTime);
        agent.isStopped = false;
    }
}