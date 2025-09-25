using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();    
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            anim.SetTrigger("Fight");
        }
        
        bool walking = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) 
                        || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        anim.SetBool("isWalking", walking);

        bool sprinting = Input.GetKey("w") && Input.GetKey("left shift");
        
        anim.SetBool("isSprinting", sprinting);

        bool backWalking = Input.GetKey(KeyCode.S);

        anim.SetBool("isWalkingBackwards", backWalking);

        bool backJogging = Input.GetKey(KeyCode.S) && Input.GetKey("left shift");

        anim.SetBool("isJoggingBackwards", backJogging);
    }
}