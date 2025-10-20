using UnityEngine;
using UnityEngine.AI;

public class EnemyPerception : MonoBehaviour
{
    /*
        this script handles the basic enemy perception
            sound: 
            sight: casts a ray every 

        it acts as a 
    */

    public GameObject player; //to get the player's last position when seen or heard
    private Rigidbody playerLocation;

    public GameObject enemy; //self, connecting to movement scripts
    public GameObject sightline; //empty object that fires out 4 rays in a v shape to act as eyeline
    public Collider listener; //a spherical collider acts as the enemy's hearing distance
    private Vector2 viewDirection;
    private Transform enemyTransform;
    private NavMeshAgent navMesh;

    private Vector3 soundOrigin;
    private Vector3 locationSpotted;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMesh = GetComponent<NavMeshAgent>();
        enemyTransform = GetComponent<Transform>();
        playerLocation = player.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 dir = playerLocation.position - enemyTransform.position;
        enemyTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
    public void HeardSound(Vector3 soundOrigin, float magnitude){
        float distance = Vector3.Distance(soundOrigin, enemyTransform.position);

        if(distance < 15f && magnitude > 0.5f){
            Debug.Log("I heard something close: " + distance + " .... with magnitude: " + magnitude);
            soundOrigin.y = 0f;
            
            navMesh.SetDestination(soundOrigin);
            
        }
        else{
            Debug.Log("Too far: " + distance + " or weak: " + magnitude);
        }
    }
}
