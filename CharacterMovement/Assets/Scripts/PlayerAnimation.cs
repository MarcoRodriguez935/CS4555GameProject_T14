using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Animator anim;

    PlayerControl player;

    void Start()
    {
        anim = GetComponent<Animator>();
        player = GetComponent<PlayerControl>();    
    }

    // Update is called once per frame
    void Update()
    {
       bool isMoving = player.movementDirection.sqrMagnitude > 0.01f;
       anim.SetBool("isWalking", isMoving && !player.isSprinting && !player.isSneaking);
       anim.SetBool("isSprinting", player.isSprinting);
       anim.SetBool("isSneaking", player.isSneaking);

       bool movingBackwards = player.movementDirection.y < -0.1f;
       anim.SetBool("isWalkingBackwards", movingBackwards && !player.isSprinting);
       anim.SetBool("isJoggingBackwards", movingBackwards && player.isSprinting);

       if (player.interact.action.WasPressedThisFrame())
       {
        anim.SetTrigger("Fight");
       }
    }
}