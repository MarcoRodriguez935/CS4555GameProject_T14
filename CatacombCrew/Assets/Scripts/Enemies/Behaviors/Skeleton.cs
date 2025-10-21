using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Skeleton : EnemyBase
{
    Cultist spawner;
    private NavMeshAgent agent;
    private float skeletonSpeed = 3.5f;
    private float skeletonLifetime = 30f;

    public override void Awake(){
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = skeletonSpeed;
        StartCoroutine(Lifetime());
    }    

    public void Init(Cultist owner){
        spawner = owner;
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
    }

    public override void OnSeen(Vector3 origin, Rigidbody playerLocation){
        seenLocation = origin;
        agent.SetDestination(seenLocation);
    }
     public virtual void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
        float distance = Vector3.Distance(origin, transform.position);
        heard = true;
        StartCoroutine(reactToSound(magnitude));
    }

    IEnumerator Lifetime(){
        yield return new WaitForSeconds(30);
        Destroy(gameObject);
    }
    void OnDestroy(){
        spawner.OnSkeletonDeath(this);
    }
}