using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Watchtower : EnemyBase
{

    /*  The watchtower is a static enemy that uses a spotlight and collider to scan an area and look for the players
        if the players are spotted by the collider inside of the spotlight, the watchtower alerts the other enemies in the level
        and feeds them the player loceation for 10 seconds; no listening
    */

    public GameObject spotLight;

    //set points to points for tower's viewing area
    public List<Transform> lightPoints;
    public Queue<Vector3> randomChecks;

    private bool focused;
    private bool alerting;
    private bool searching;
    private GameObject playerLock;
    public Transform[] otherEnemies;

    public override void Awake(){

  
    }

    public override void Update(){
        base.Update();
 
    }

    //rotate light (eyes object) to next point object
    public void pointLight(){

    }
  
}