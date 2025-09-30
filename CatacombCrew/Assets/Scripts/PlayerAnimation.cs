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
    //    bool isMoving = player.movementDirection.sqrMagnitude > 0.01f;
    //    anim.SetBool("isWalking", isMoving && !player.isSprinting && !player.isSneaking);
    //    anim.SetBool("isSprinting", player.isSprinting);
    //    anim.SetBool("isSneaking", player.isSneaking);

    //    bool movingBackwards = player.movementDirection.y < -0.1f;
    //    anim.SetBool("isWalkingBackwards", movingBackwards && !player.isSprinting);
    //    anim.SetBool("isJoggingBackwards", movingBackwards && player.isSprinting);

    //    if (player.interact.action.WasPressedThisFrame())
    //    {
    //     anim.SetTrigger("Fight");
    //    }

        // ------------ GPT Fix -----------------
        // So that if the player looks backwards with the mouse, that direction becomes the new forward
        // and S will play the walk forward animation

        bool isMoving = player.movementDirection.sqrMagnitude > 0.01f;

        Vector3 moveInput = new Vector3(player.movementDirection.x, 0f, player.movementDirection.y);
        
        Vector3 camForward = player.mainCam.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = player.mainCam.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveWorld = camForward * moveInput.z + camRight * moveInput.x;

        Vector3 localMove = transform.InverseTransformDirection(moveWorld);

        anim.SetBool("isWalking", isMoving && !player.isSprinting && !player.isSneaking && localMove.z > 0.1f);
        anim.SetBool("isWalkingBackwards", isMoving && !player.isSprinting && localMove.z < -0.1f);
        anim.SetBool("isSprinting", player.isSprinting && localMove.z > 0.1f);
        anim.SetBool("isJoggingBackwards", player.isSprinting && localMove.z < -0.1f);
        anim.SetBool("isSneaking", player.isSneaking);

        anim.SetBool("isWalkingRight", isMoving && Mathf.Abs(localMove.x) > 0.1f && localMove.x > 0f && !player.isSprinting);
        anim.SetBool("isWalkingLeft", isMoving && Mathf.Abs(localMove.x) > 0.1f && localMove.x < 0f && !player.isSprinting);

        if (player.interact.action.WasPressedThisFrame())
        {
            anim.SetTrigger("Fight");
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetTrigger("Jump");
        }

        // Roll (V key)
        if (Input.GetKeyDown(KeyCode.V))
        {
            anim.SetTrigger("Roll");
        }


    }
}