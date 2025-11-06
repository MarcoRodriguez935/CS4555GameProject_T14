using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RatNest : MonoBehaviour
{

    /* Ratnest script is used to create/destroy giant rat enemies, gives them new wandering routes
        and alerts them when the player is inside of the  
    */

    public GiantRats ratPrefab;
    public List<Transform> allPoints; //all possible wander points

    public float spawnDespawnInterval = 5f;
    private float spawnChance = 0.2f;
    private int minRats = 1;
    private int maxRats = 4;
    private float swarmRadius = 3f;
    private float destroyTime = 5f;

    private List<GiantRats> ratList = new List<GiantRats>();
    private HashSet<GiantRats> inNest = new HashSet<GiantRats>();
    private Vector3 nestLocation;
    private LayerMask playerMask;
    private System.Random rng = new System.Random();

    private float nextToggleAt;
    private bool torchOnNest;
    private float burnTimer = 0;

    void Start()
    {
        nestLocation = transform.position;
        playerMask = LayerMask.GetMask("Player");

        var found = GetComponentsInChildren<GiantRats>(true);

        foreach (var rat in found.Take(maxRats)){
            ratList.Add(rat);
            rat.Initialize(this, GetWanderRoute());
        }

        if(found.Length == 0 && ratPrefab != null){
            int toSpawn = Random.Range(minRats, maxRats + 1);
            for(int i = 0; i < toSpawn; i++){
                Spawn();
            }
        }

        nextToggleAt = Time.time + spawnDespawnInterval;
    }

    // Update is called once per frame
    void Update()
    {
        if(torchOnNest){
            Debug.Log("Nest is Burning!");

            burnTimer += Time.deltaTime;
            if(burnTimer >= destroyTime) DestroyNest();
        }
        else if(burnTimer > 0f){
            burnTimer = Mathf.Max(0f, burnTimer - Time.deltaTime);
        }

        if(Time.time >= nextToggleAt){
            TickSpawnDespawn();
            nextToggleAt = Time.time + spawnDespawnInterval;
        }
    }

    public Queue<Vector3> GetWanderRoute(){
        Queue<Vector3> queue = new Queue<Vector3>();
        int take = Mathf.Clamp(3 + rng.Next(3), 1, allPoints.Count);
        foreach(var point in allPoints.Where(point => point != null).OrderBy(_ => rng.Next()).Take(take))
            queue.Enqueue(point.position);
        
        queue.Enqueue(nestLocation);
        return queue;
    }

    public Vector3 GetOppositePoint(Vector3 playerPosition){
        Vector3 best = nestLocation;
        float bestDist = -1f;
        foreach(var point in allPoints){
            float distance = (point.position - playerPosition).sqrMagnitude;
            if(distance > bestDist){
                bestDist = distance;
                best = point.position;
            }
        }
        return best;
    }

    public void TriggerSwarm(Vector3 playerPosition){
        foreach(var rat in ratList){
            if(rat == null) continue;
            bool closeBy = Vector3.Distance(rat.transform.position, nestLocation) <= swarmRadius;
            if(closeBy || inNest.Contains(rat)) rat.Swarm(playerPosition);
        }
    }

    public void TriggerSwarm(Transform player){
        foreach(var rat in ratList){
            if(rat == null) continue;
            bool closeBy = Vector3.Distance(rat.transform.position, nestLocation) <= swarmRadius;
            if(closeBy || inNest.Contains(rat)) rat.Swarm(player);
        }
    }

    private void TickSpawnDespawn(){
        ratList = ratList.Where(rat => rat != null).ToList();
        inNest.RemoveWhere(rat => rat == null);

        int alive = ratList.Count;
        if(alive < minRats){
            Spawn();
            return;
        }
        if(alive > maxRats){
            Despawn();
            return;
        }

        if(Random.value < spawnChance) Spawn();
        else Despawn();

    }

    private void Spawn(){
        if(ratList.Count >= maxRats) return;

        var rat = Instantiate(ratPrefab, nestLocation, Quaternion.identity);
        rat.transform.SetParent(transform, true);
        rat.Initialize(this, GetWanderRoute());
        ratList.Add(rat);
    }

    private void Despawn(){
        var rats = inNest.Where(rat => rat != null).ToList();
        if(rats.Count == 0) return;
        rats[rng.Next(rats.Count)].TryToggleDespawn();
    }

    void DestroyNest(){
        foreach(var rat in ratList){
            Destroy(rat.gameObject);
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other){
        var rat = other.GetComponentInParent<GiantRats>();
        if(rat && ratList.Contains(rat)){
            inNest.Add(rat);
        }
    }

    void OnTriggerExit(Collider other){
        var rat = other.GetComponentInParent<GiantRats>();
        if(rat) inNest.Remove(rat);

        if(other.CompareTag("Torch")){
            torchOnNest = false;
        }
    }
    void OnTriggerStay(Collider other){
        if(other.CompareTag("Torch")){
            torchOnNest = true;
            return;
        }
        var rat = other.GetComponentInParent<GiantRats>();
        if(rat && ratList.Contains(rat)){
            inNest.Add(rat);
        }
    }

    void OnDisable(){
        foreach(var rat in ratList){
            if (rat) rat.ForceUnhide();
        }
    }
}