using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeAnimation : MonoBehaviour
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
        if ((Input.GetKey(KeyCode.W)) || (Input.GetKey(KeyCode.A)) || (Input.GetKey(KeyCode.S)) || (Input.GetKey(KeyCode.D)))
        {
            anim.SetTrigger("Walk");
        }
        else
        {
            anim.SetTrigger("Idle");
        }
    }
}