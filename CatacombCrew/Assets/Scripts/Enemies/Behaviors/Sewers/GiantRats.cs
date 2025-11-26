using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GiantRats : EnemyBase
{

    /*  Giant Rats are found in the sewers; they go to nests and wander around the sewers periodically
        If the players get close to them while they are wandering, they will attack once before fleeing to the nest
        if they are seen/heard from afar, the rats will simply move to another point in the area that doesn't cross the player
        If the players get close to the nests, all of the rats present at/close to the nest will chase them for some time
            the players will be given hiding places (trap setup/pressure plate) ; rats stop swarming after some time of silence
    */

    private RatNest nest;
    private Queue<Vector3> wanderPoints = new Queue<Vector3>();
    private Vector3 currentPoint;
    private Transform swarmTarget; 
    private Vector3 lastKnownPos;

    private Transform target;
    private bool singleAttack;
    private float attackRange = 1.1f;
    private float lungeSpeed = 10f;
    private float lungeTime = 0.25f;

    private float swarmAttackCooldown = 0.75f;
    private float nextSAttack = 0f;

    private enum Mode { Wandering, Burrowing, Swarming, Fleeing }
    private Mode mode;

    private float walkSpeed = 2f;
    private float runSpeed = 4.5f;
    private float detectRadius = 3f; //range for players to be inside of nest to be swarmed
    private float interestTimer = 5f; //seconds of silence/no LOS before the rats stop swarming
    private float burrowUntil;

    private bool hidden;
    private float lastStimulusTime = -1f;

    private RatSounds sounds;

    public override void Awake(){
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;
        agent.avoidancePriority = Random.Range(30, 70);
        sightDistance = 8f;
        sounds = GetComponent<RatSounds>();
    }

    public override void Update(){
        base.Update();

        switch(mode){
            case Mode.Wandering:
                if(!agent.pathPending && agent.remainingDistance <= 0.2f){
                    AdvanceRouteOrBurrow();
                }
                break;

            case Mode.Burrowing:
                agent.isStopped = true;
                if(Time.time >= burrowUntil){
                    AssignWander(nest.GetWanderRoute());
                }
                break;

            case Mode.Swarming: 
                agent.isStopped = false;
                agent.speed = runSpeed;
                Vector3 dest = swarmTarget ? swarmTarget.position : lastKnownPos;
                if(swarmTarget) lastKnownPos = dest;
                agent.SetDestination(dest);

                if(Vector3.Distance(transform.position, dest) <= attackRange){
                    SwarmAttack();
                    break;
                }
                if(Time.time - lastStimulusTime > interestTimer){
                    ReturnToNest();
                }
                break;

            case Mode.Fleeing: 
                if(!agent.pathPending && agent.remainingDistance <= 0.2f)
                    AssignWander(nest.GetWanderRoute());
                break;
        }
    }

    public override void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
        lastStimulusTime = Time.time;

        Transform player = null;
        if(reason){
            var rigidb = reason.GetComponentInParent<Rigidbody>();
            if(rigidb) player = rigidb.transform;
            else if(reason.CompareTag("Player")) player = reason.transform.root;
        }

        if(player){
            swarmTarget = player;
            lastKnownPos = swarmTarget.position;
        }
        else{
            swarmTarget = null;
            lastKnownPos = origin;
        }

        if(nest && Vector3.Distance(transform.position, nest.transform.position) <= detectRadius){
            nest.TriggerSwarm(swarmTarget ? swarmTarget : null);
        }

        if(mode == Mode.Wandering){
            float distance = Vector3.Distance(origin, transform.position);
            if(distance <= detectRadius + 1f){
                ShortAttack(origin);
            }
            else{
                SkipPointsNear(origin);
            }
        }
    }

    public override void OnSeen(Vector3 origin, Rigidbody playerLocation){
        base.OnSeen(origin, playerLocation);
        lastStimulusTime = Time.time;

        if(playerLocation){
            swarmTarget = playerLocation.transform;
            lastKnownPos = swarmTarget.position;
        }
        else{
            lastKnownPos = origin;
        }

        if(nest && Vector3.Distance(transform.position, nest.transform.position) <= detectRadius){
            nest.TriggerSwarm(swarmTarget ? swarmTarget : null);
        }

        if(mode == Mode.Wandering){
            float distance = Vector3.Distance(origin, transform.position);
            if(distance <= attackRange){
                ShortAttack(lastKnownPos);
                return;
            }
            else{
                SkipPointsNear(origin);
            }
        }
    }

    public void Initialize(RatNest home, Queue<Vector3> wanderRoute = null){
        nest = home;
        agent.GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

        if(wanderRoute == null && nest != null){
            wanderRoute = nest.GetWanderRoute();
        }

        AssignWander(wanderRoute);

    }

    public void AssignWander(Queue<Vector3> route){
        singleAttack = false;
        wanderPoints = route;
        mode = Mode.Wandering;
        hidden = false;
        ForceUnhide();
        AdvanceRouteOrBurrow();
    }

    public void Swarm(Vector3 target){
        //if players inside of nest's range and a rat sees them,
        //all the rats inside the nest and around will swarm for 20s
        agent.isStopped = false; 
        agent.speed = runSpeed;
        swarmTarget = null;
        lastKnownPos = target;
        lastStimulusTime = Time.time;
        mode = Mode.Swarming;
    }   

    public void Swarm(Transform target){
        agent.isStopped = false; 
        agent.speed = runSpeed;
        swarmTarget = target;
        if (target) lastKnownPos = target.position;
        lastStimulusTime = Time.time;
        mode = Mode.Swarming;
    }

    public void TryToggleDespawn(){
        if(mode != Mode.Burrowing) return;
        hidden = !hidden;
        ToggleVisible(!hidden);
    }

    public void ForceUnhide(){
        hidden = false;
        ToggleVisible(true);
    }

    private void AdvanceRouteOrBurrow(){
        if(wanderPoints.Count > 0){
            currentPoint = wanderPoints.Dequeue();
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(currentPoint);
        }
        else{
            mode = Mode.Burrowing;
            burrowUntil = Time.time + Random.Range(2f, 6f);
            agent.isStopped = true;
            ToggleVisible(false);
        }
    }

    private void ShortAttack(Vector3 playerPosition){
        if(singleAttack) return;
        singleAttack = true;
        StartCoroutine(LungeThenFlee(playerPosition));
    }

    private IEnumerator LungeThenFlee(Vector3 playerPosition){
        agent.isStopped = false;
        float prevSpeed = agent.speed;
        float prevAccel = agent.acceleration; 
        float prevAng = agent.angularSpeed;

        agent.speed = lungeSpeed;
        agent.acceleration = 100f;
        agent.angularSpeed = 720f;

        Vector3 direction = (playerPosition - transform.position).normalized;
        Vector3 lungePoint = playerPosition + direction * 0.3f;
        agent.SetDestination(lungePoint);

        yield return new WaitForSeconds(lungeTime);

        sounds.PlayAttack();

        agent.speed = prevSpeed;
        agent.acceleration = prevAccel;
        agent.angularSpeed = prevAng;

        FleeFrom(playerPosition);
    }

    private void SwarmAttack(){
        if(Time.time < nextSAttack) return;
        sounds.PlayAttack();
        nextSAttack = Time.time + swarmAttackCooldown;
    }

    private void FleeFrom(Vector3 playerPosition){ //attack and then run
        Vector3 fleePoint = nest.GetOppositePoint(playerPosition);
        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(fleePoint);
        mode = Mode.Fleeing;
    }

    private void SkipPointsNear(Vector3 position){
        if(Vector3.Distance(currentPoint, position) < detectRadius * 1.5f){
            AdvanceRouteOrBurrow();
            return;
        }
        if(wanderPoints.Count == 0) return;

        var list = wanderPoints.ToList();
        int index = -1;
        float best = float.MaxValue;
        for(int i = 0; i < list.Count; i++){
            float distance = Vector3.Distance(list[i], position);
            if(distance < best){
                best = distance;
                index = i;
            }
        }
        if(index >= 0 && best < detectRadius * 1.5f){
            list.RemoveAt(index);
        }
        wanderPoints = new Queue<Vector3>(list);
    }

    private void ReturnToNest(){
        singleAttack = false;

        if(nest){
            wanderPoints = new Queue<Vector3>(new[] { nest.transform.position} );
            mode = Mode.Wandering;
            AdvanceRouteOrBurrow();
        }
        else{
            mode = Mode.Wandering;
        }
    }

    private void ToggleVisible(bool on){
        var rends = GetComponentsInChildren<Renderer>(true);
        foreach (var rat in rends){
            rat.enabled = on;
            var collider = GetComponent<Collider>();
            if(collider) collider.enabled = on;
        }
    }
}