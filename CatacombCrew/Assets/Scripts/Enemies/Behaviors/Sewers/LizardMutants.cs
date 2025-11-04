// using UnityEngine;
// using UnityEngine.AI;
// using System.Collections;
// using System.Collections.Generic;

// public class LizardMutants : EnemyBase
// {

//     // /*  LizardMutants patrol areas in the sewers; 
//     // if they hear or see the player, they will begin to stalk them at a distance
//     //     the players must scare them away by looking at them and moving at them
//     //     if they fail to do this and 3 lizards are stalking at the same time, 
//     //         they will attack the player all at once and chase for a short period of time
//     // */

//     private float walkSpeed = 2f;
//     private float chargeSpeed = 5f; 
//     private float investigateFor = 15f; //time spent patrolling a room sent to investigate in

//     private float detectRadius = 3f; //how close the players can be before they are detected without making noise
//     private float slamRadius = 5f; //radius of the slam attack performed at the end of a charge

//     public Transform[] patrolPoints;
//     private int patrolDest = 0;
//     private int currentDest = -1;

//     private bool charging;
//     private bool investigating;
//     private bool escorting;

//     //needs to prioritize sounds that it hears so it focuses on just one
//     private float focusedPriority;
//     private Vector3 focusedSoundPos;
//     private GameObject playerLock;

//     //preventing listening ray spam due to large collider
//     float listeningCooldown = 0.5f;
//     float muteTime = 0f;


//     public override void Awake(){

//         agent = GetComponent<NavMeshAgent>();
//         agent.speed = walkSpeed;
//         agent.avoidancePriority = 75;
//         agent.autoBraking = true;
//         agent.stoppingDistance = 0.5f;
//         stunned = false;

//         ToNextRoom();
//     }

//     public override void Update(){
//         //no base.Update as they are blind;
//         if(escorting || charging || stunned || agent == null) return;

//         if(!investigating && !charging && !agent.pathPending && agent.remainingDistance < 0.5f && !agent.isStopped){
//             ToNextRoom();
//         } 
//     }

//     public override void OnSound(Vector3 origin, Vector3 currentDir, float magnitude, GameObject reason){
//         if(escorting) return;

//         if(Time.time < muteTime) return;
//         muteTime = Time.time + listeningCooldown;


//         float distance = Vector3.Distance(origin, transform.position);
//         float priority = magnitude / Mathf.Max(1f, distance);

//         //going to be hearing a lot of sounds, focus on the loudest one instead of getting stuck on just one
//         if(priority > focusedPriority){
//             focusedPriority = priority; 
//             focusedSoundPos = origin;
//         }

//         StartCoroutine(reactToSound(magnitude));
//         heardPlayer = true;
//         playerLock = reason;

//         if(!investigating){ //if investigating, teleport halfway to the sound source and patrol
//             Vector3 halfwayPoint = Vector3.Lerp(transform.position, origin, 0.5f);
//             agent.Warp(halfwayPoint);
//             agent.speed = walkSpeed;
//             focusedSoundPos = origin;
//             StartCoroutine(Investigate());
//         }
//         else{ //if the player makes another noise close by during investigation; charge/slam
//             if(distance <= detectRadius){
//                 focusedSoundPos = origin;
//                 StartCoroutine(ChargeAndSlam(focusedSoundPos));
//             }
//             else{
//                 ClearRoom(origin);
//             }
//         }
//     }

//     Queue<Vector3> GetRoomPatrols(Vector3 around){
//         Queue<Vector3> queue = new Queue<Vector3>();
//         GameObject[] rooms = GameObject.FindGameObjectsWithTag("Room");
//         GameObject nearestRoom = null;
//         float best = float.PositiveInfinity;
//         foreach (var room in rooms){
//             float d = (room.transform.position - around).sqrMagnitude;
//             if(d < best){
//                 best = d;
//                 nearestRoom = room;
//             }
//         }

//         if(nearestRoom != null){
//             foreach(Transform child in nearestRoom.transform){
//                 queue.Enqueue(child.position);
//             }
//         }
//         return queue;
//     }
// }