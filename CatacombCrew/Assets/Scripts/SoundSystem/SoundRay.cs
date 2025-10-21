using UnityEngine;
using System.Collections.Generic;

public static class SoundRay    {
    /* 
        Custom ray class to handle the magnitude, bounces and decay over distance
            handles collisions & reflections with walls and obstacles in rooms
            alerts enemy when a collision is detected; no reflection
    */

    public static void fireRay(Vector3 origin, Vector3 direction, float magnitude, float rayDecay, int layerMask, GameObject source){
        int bounceCount = 0;
        int maxBounces = 2;

        Vector3 currentPos = origin;
        Vector3 currentDir = direction;
        float currentStrength = magnitude;

       //kill the ray if it goes over the maximum amount of bounces or when the magnitude is negligible
        while(bounceCount <= maxBounces && currentStrength > 0.1f){
            if(Physics.Raycast(currentPos, currentDir, out var rayHit, currentStrength, layerMask, QueryTriggerInteraction.Collide)){
                //wall hits count for bounces and muffle noises greatly
                if(rayHit.collider.CompareTag("Enemy")){
                    currentPos = rayHit.point + currentDir * 0.01f;;
                    currentStrength = currentStrength * rayDecay;

                    var reaction = rayHit.collider.GetComponentInParent<EnemyBase>();
                    if(reaction != null){
                        reaction.OnSound(origin, currentDir, currentStrength, source);
                    }

                    Debug.DrawRay(currentPos, currentDir * currentStrength, Color.red, 1f);
                }

                else if(rayHit.collider.CompareTag("Wall")){
                    currentPos = rayHit.point + rayHit.normal * 0.01f;

                    float variance = Random.Range(-5f, 5f);
                    Quaternion rotation = Quaternion.AngleAxis(variance, Vector3.up);
                    currentDir = rotation * currentDir;
                    currentDir = Vector3.Reflect(currentDir, rayHit.normal);

                    bounceCount++;
                    currentStrength = currentStrength * (rayDecay * .85f);

                    Debug.DrawRay(currentPos, currentDir * currentStrength, Color.blue, 2f);
                    
                }

                //obstacle hits don't count for bounces and only muffle noises slightly
                else if(rayHit.collider.CompareTag("Obstacle")){
                    currentPos = rayHit.point + currentDir * 0.01f;;
                    currentDir = Vector3.Reflect(currentDir, rayHit.normal);
                    currentStrength = currentStrength * (rayDecay * .95f);

                    Debug.DrawRay(currentPos, currentDir * currentStrength, Color.yellow, 2f);

                }
                else{
                    break;
                }
                
            } 
            else{
                break;
            }
        }
    }
}