using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class RatNest : MonoBehaviour
{

    private List<GiantRats> ratList;
    private List<GiantRats> inNest;
    public float spawnDespawnInterval = 5f;
    private int ratCount = 0;

    public List<Vector3> allPoints; //all possible wander points
    private Queue<Vector3> wanderPoints; //passed to rats for pathing, randomized

    private Vector3 nestLocation;
    private LayerMask playerDetect;
    private bool playerDetected;
   
    private float burnTimer = 0;
    private bool destroySelf; //if players drop torch on top of nest, timer for destruction
    private float destroyTime = 30f;

    void Start()
    {
        ratList = new List<GiantRats>();
        nestLocation = transform.position;
        playerMask = LayerMask.GetMask("Player");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnDespawnRats(){
        if()
        ratList.Add(rat);
        rat.Initialize(this);
        rat.AssignWander(GetWanderRoute());
        rat.StartWander();
    }

    Queue<Vector3> GetWanderRoute(){
        wanderPoints = new Queue<Vector3>();
        var shuffled = allPoints.OrderBy(_ => _rand.Next()).ToList();
        
        var getPoints = shuffled.Take(4).ToList();
        foreach (var point in getPoints){
            wanderPoints.Enqueue(point);
        }
        wanderPoints.Enqueue(nestLocation);
        return wanderPoints;
    }

    IEnumerator burnNest(){
        burnTimer += Time.deltaTime;
        if(burnTimer >= destroyTime) DestroyNest();
    }

    void DestroyNest(){
        foreach(var rat in inNest){
            rat.Destroy();
        }
    }
    void OnTriggerEnter(Collider other){
        if(other.gameobject.layer == playerMask.value){
            playerDetected = true;
        }
        var rat = other.GetComponentInParent<GiantRats>();
        if(rat) inNest.Add(rat);
    }

    void OnTriggerExit(Collider other){
        if(other.gameobject.layer == playerMask.value){
            playerDetected = false;
        }
        var rat = other.GetComponentInParent<GiantRats>();
        if(rat) inNest.Remove(rat);
    }
    void OnTriggerStay(Collider other){
        if(other.CompareTag("Torch")){
            StartCoroutine(burnNest());
        }
    }

}
