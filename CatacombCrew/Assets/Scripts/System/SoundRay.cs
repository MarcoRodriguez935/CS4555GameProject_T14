using UnityEngine;
using System.Collections.Generic;

public class SoundRay : MonoBehaviour
{
    /* 
        Custom ray class to handle the magnitude, bounces and decay over distance
            handles collisions & reflections with walls and obstacles in rooms
            alerts enemy when a collision is detected; no reflection
    */

    public Vector3 origin;
    public Vector3 direction;
    public float magnitude;
    private int bounceCount;
    public int maxBounces = 2;
    public float rayDecay = .75f;
    public List<RaycastHit> hitHistory = new List<RaycastHit>();

    public SoundRay(){

    }

    public SoundRay(Vector2 origin, Vector3 direction, float magnitude){
        this.origin = origin;
        this.direction = direction;
        this.magnitude = magnitude;
    }

    public void fireRay(){
        int layerMask = LayerMask.GetMask("Wall", "Obstacle");

        bounceCount = 0;
        Vector3 currentPos = origin;
        Vector3 currentDir = direction;
        float currentStrength = magnitude;

       //kill the ray if it goes over the maximum amount of bounces or when the magnitude is negligible
        while(bounceCount < maxBounces && currentStrength > 0f){

            RaycastHit rayHit;
            bool hitDetected = Physics.Raycast(currentPos, currentDir, out rayHit, currentStrength, layerMask);

            //detect hits, only reflect if the hits are walls or obstacles
            if(hitDetected && rayHit.collider != null){
                hitHistory.Add(rayHit);

                // //when a ray hits an enemy, alert & pass to behavior tree script
                // if(rayHit.collider.CompareTag("Enemy")){
                //     rayHit.collider.GetComponent<EnemyAI>()?.OnSoundHeard(rayHit.point);
                // }
                
                if(rayHit.collider.CompareTag("Wall")){
                    currentPos = rayHit.point + rayHit.normal * 0.01f;

                    float variance = Random.Range(-5f, 5f);
                    Quaternion rotation = Quaternion.AngleAxis(variance, Vector3.up);
                    currentDir = rotation * currentDir;
                    currentDir = Vector3.Reflect(currentDir, rayHit.normal).normalized;

                    bounceCount++;
                    currentStrength = currentStrength * (rayDecay * .75f);
                     //wall hits count for bounces and muffle noises greatly
                    Debug.DrawRay(currentPos, currentDir * currentStrength, Color.blue);
                    
                }

                //obstacle hits don't count for bounces and only muffle noises slightly
                if(rayHit.collider.CompareTag("Obstacle")){
                    currentPos = rayHit.point;
                    currentStrength = currentStrength * (rayDecay * .95f);
                    Debug.DrawRay(currentPos, currentDir * currentStrength, Color.red);

                }
            } else{
                //ray decay over time and drawing
                Debug.DrawRay(currentPos, currentDir * currentStrength, Color.green);
                currentStrength = currentStrength * rayDecay;
                break;
            }
        }
        Destroy(gameObject);
    }
}