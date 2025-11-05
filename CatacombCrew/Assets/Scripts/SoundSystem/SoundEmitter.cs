using UnityEngine;
using System.Collections;

public class SoundEmitter : MonoBehaviour
{
    /*
    This script will be used to assign and create the sound ray types that the player, items, or puzzles emit
        Player: player footsteps, item usage, sprinting/sneaking
        
        using custom ray class that tracks the position and collisions

        A separate script will be applied to enemy prefabs to detect collision from these rays using their magnitude 
            which will then trigger an event in their behavior tree (investigate, charge, run, etc)
    */
    public GameObject emitter; //can be player, item, or puzzle/interactable -- dependent on tag of object
    private Transform emitTransform;
    private int directionCount = 16;
    float magnitude;

    private bool isPlayer = false;
    private Rigidbody playerRb;
    private PlayerControl playerScript;

    private float emissionRate;
    private float lastEmissionTime = 0f;
    private LayerMask mask;

    void Start()
    {
        mask = LayerMask.GetMask("Wall", "Obstacle", "Enemy");
        assignRayType(emitter.tag);
        emitTransform = emitter.transform;
        if(isPlayer){
            playerRb = emitter.GetComponentInParent<Rigidbody>();
            playerScript = GetComponentInParent<PlayerControl>();
        } 
    }

    void FixedUpdate()
    {   
        if(!isPlayer){
            lastEmissionTime += Time.fixedDeltaTime;
            if(lastEmissionTime <= emissionRate){
                emitSoundRays();
                lastEmissionTime = 0f;
            }
        }
        else{
            lastEmissionTime += Time.fixedDeltaTime;
            if(playerRb.linearVelocity.magnitude > 0.01f){
                float originalMagnitude = magnitude;
                if(lastEmissionTime >= emissionRate){
                    if(playerScript.isSprinting){
                        magnitude = magnitude * 1.25f;
                    }
                    else if(playerScript.isSneaking){
                        magnitude = magnitude * 0.15f;
                    } 
                    emitSoundRays();
                    magnitude = originalMagnitude;
                    lastEmissionTime = 0f;
                }
            }
        }
    }

    //sets the magnitude of each ray dependant on the type of origin
    void assignRayType(string type){
        switch(type){
            case "Player":
                isPlayer = true;
                emissionRate = 1.5f;
                magnitude = 10f;
                break;
            case "Item":
                emissionRate = 1f;
                magnitude = 40f;
                break;
            case "Puzzle": 
                emissionRate = 2f;
                magnitude = 50f;
                break;  
        }
    }
    void emitSoundRays(){
        for(int i = 1; i <= directionCount; i++){
            float rayAngle = i * (360f / directionCount) * Mathf.Deg2Rad;
            Vector3 origin = emitTransform.position;
            Vector3 direction = new Vector3(Mathf.Cos(rayAngle), 0f, Mathf.Sin(rayAngle));
            SoundRay.fireRay(origin, direction, magnitude, .5f, mask, this.gameObject);
        }
    }
}