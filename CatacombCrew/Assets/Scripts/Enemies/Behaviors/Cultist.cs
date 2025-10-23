using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Cultist : EnemyBase
{
    public Transform[] patrolPoints;
    private int patrolDest = 0;
    private int currentDest = -1;

    public GameObject skeletonPrefab;
    private int maxSkeletons = 6;
    protected int skeletonCount;
    private float spawnRadius = 2f;

    private bool onCooldown;
    private bool communing;
    private bool escorted;
    private bool rushing;
    private bool sweeping;
    private bool panic;

    private CultistAnimation cultistAnimation;
    
    private float baseSpeed = 2f;


    public override void Awake(){
        cultistAnimation = GetComponent<CultistAnimation>();
        base.Awake();
        skeletonCount = 0;
        sightDistance = 6f;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = baseSpeed;
        agent.avoidancePriority = 50;
        agent.autoBraking = true;
        agent.stoppingDistance = 0.5f;
        panic = false;
        blind = false;
        stunned = false;
        ToNextRoom();
    }   

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
        if(stunned || agent == null) return;

        if(!agent.pathPending && agent.remainingDistance < 0.5f && !agent.isStopped && !sweeping){
            if(currentDest >= 0 && patrolPoints[currentDest].CompareTag("PatrolPause")){
               StartCoroutine(Commune());
            }
            else{
                ToNextRoom();
            }
        } 

        if(sawPlayer){   
            float distance = Vector3.Distance(seenLocation, transform.position);
            if(distance <= sightDistance){
                PanicSweep();
            }
        }
    }
    void ToNextRoom(){ //patrolling behavior
        communing = false;
        if(patrolPoints.Length == 0)
            return;

        currentDest = patrolDest;

        agent.destination = patrolPoints[currentDest].position;
        patrolDest = UnityEngine.Random.Range(0, patrolPoints.Length);

        if(rushing && agent.remainingDistance < 0.5f){
            rushing = false;
            heardPlayer = false;
            agent.speed = baseSpeed;
        }
    }
    IEnumerator Commune(){ //bug not a feature type shi, when communing hearing/sight is ignored
        if(communing) yield break;
        communing = true;

        agent.isStopped = true;
        Debug.Log("Cultist is communing with the spirits!");
        yield return new WaitForSeconds(5);
        agent.isStopped = false;
        communing = false;

        ToNextRoom();
    }

    //sound alerts, far / close
    IEnumerator Rush(){ //alerted by far sound, rush to next point
        if(rushing) yield break;
        rushing = true;

        if(panic){
            agent.speed *= 3f;
        }
        else{
            agent.speed *= 2f;
        }
        Debug.Log("Increasing agent speed: " + agent.speed); 

        yield return null;
    }
    IEnumerator Sweep(Vector3 soundDirection, float angle = 60f, float duration = 1.25f){ //alerted by close sound, vision sweep area before rushing or summoning on contact
        if(sweeping) yield break;

        sweeping = true;
        agent.updateRotation = false;
        agent.isStopped = true;

       try{
            Vector3 toSound = soundDirection - transform.position;
            toSound.y = 0f;
            Quaternion faceSound = Quaternion.LookRotation(toSound);

            yield return LookToSound(faceSound, duration);
            if(sawPlayer && !panic){
                agent.isStopped = false;
                StartCoroutine(Rush());
                yield break;
            }

            yield return LookToSound(faceSound * Quaternion.Euler(0f, -angle, 0f), duration);
            if(sawPlayer && !panic){
                StartCoroutine(Rush());
                yield break;

            }

            yield return LookToSound(faceSound * Quaternion.Euler(0f, angle, 0f), duration);
            if(sawPlayer && !panic){
                StartCoroutine(Rush());
                yield break;
            }
       } finally{

            if(panic){
                sawPlayer = false;
                panic = false;
            }

            agent.isStopped = false;
            agent.updateRotation = true;
            sweeping = false;
            heardPlayer = false;
       }   
    }
     IEnumerator LookToSound(Quaternion target, float duration){
        Quaternion start = transform.rotation;
        float t = 0f;
        while(t < duration){
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, target, t / duration);
            yield return null;
        }
    }
    
    //player seen
    void PanicSweep(){ //cultist will summon 3 skeletons, & TODO run to LD, then rush for next 3 points;
        if(panic) return;

        Debug.Log("Cultist panicked!");
        panic = true;
        StartCoroutine(reactToSight(seenLocation));
        Summon(3);
        StartCoroutine(Sweep(seenLocation, 75f, .25f));
        StartCoroutine(Rush());
    }

    IEnumerator EndPanic(){
        yield return new WaitForSeconds(.25f);
        panic = false;
        sawPlayer = false;
    }

    //summons 3 skeletons that patrol the room they are spawned in
    void Summon(int count){ 
        if(onCooldown) return;
        if(skeletonCount >= maxSkeletons) return;
        {
            Debug.Log("Summoning Skeletons");
            cultistAnimation.SummonAnim();
            for(int i = 0; i < count; i++){
                Vector3 raw = transform.position + Random.insideUnitSphere * spawnRadius;
                if (NavMesh.SamplePosition(raw, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    GameObject go = Instantiate(skeletonPrefab, hit.position, Quaternion.identity);
                    Skeleton skeleton = go.GetComponent<Skeleton>();
                    skeleton.Init(this);
                    skeletonCount++;
                    // Debug.Log("Skeleton Count: " + skeletonCount);
                }
            }        
        }
        StartCoroutine(SummonCooldown());
    }
    IEnumerator SummonCooldown(){
        onCooldown = true;
        yield return new WaitForSeconds(15);
        onCooldown = false;
        // Debug.Log("Cooldown finished");
    }

    public void OnSkeletonDeath(Skeleton sk){
        skeletonCount--;
    }

    public override void OnSound(Vector3 origin, Vector3 dir, float magnitude, GameObject reason){
        float distance = Vector3.Distance(origin, transform.position);
        
        if(communing || escorted) return;

        //if the sound is loud enough to hear, and far: rush; else sweep with vision
        if(magnitude >= 5f){
            Debug.Log("heard a sound in: " + distance + " units, with magnitude: " + magnitude);
            if(distance <= 8f){
                heardPlayer = true;
                StartCoroutine(reactToSound(magnitude));
                Debug.Log("sweeping");
                StartCoroutine(Sweep(origin));
            } 
            else if(distance <= 15f && !heardPlayer && !rushing){
                heardPlayer = true;
                if(!rushing){
                    StartCoroutine(reactToSound(magnitude));
                    Debug.Log("rushing");
                    StartCoroutine(Rush());
                }
            }
        }
    }
}