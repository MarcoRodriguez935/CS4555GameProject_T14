using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour, SoundHeard, PlayerSeen
{

    /* abstract class to handle basic anamy movement, processing stimuli, driving behavior
        every enemy has a transform, reactiontime, navMesh
        some enemies can be blind, stunned, patrolroutes, investigations or panics (bools)
    */

    public GameObject self;
    public Transform selfTransform;
    public PatrolRoute route;
    public float reactionTime;
    private NavMeshAgent agent;
    private Vector3 checkLocation;
    private bool investigating;
    private bool stunned;

    //virtual keyword allows the methods to be overridden?
    public virtual void OnSound(Vector3 origin, float magnitude, GameObject reason){
        float distance = Vector3.Distance(origin, selfTransform.position);
        investigating = true;
        if(distance < 10f && magnitude > 3f){
            checkLocation = origin;
        }
    }

    public virtual void OnSeen(GameObject player){
        float distance = Vector3.Distance(origin, selfTransform.position);
        investigating = false;
        checkLocation = origin;
    }

    public virtual void Update(){
        if(investigating && agent) agent.SetDestination(checkLocation);  
    }
}
