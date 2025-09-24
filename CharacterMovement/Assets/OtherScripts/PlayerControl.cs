using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{

    public Rigidbody rb;

    //player modifiers [items, status effects, defaults]
    private float playerSpeed = 3f;
    private float jumpForce = 3f;
    private float diveForce = 50f;
    // private float turnSpeedinDeg = 120f; //if we use a quaternion then it's for turn speed; might need one b/c controller and keyboard differences

    //action cooldowns [diving, item usage, status effects]
    private float diveCooldown = 2.5f;
    private float lastDiveTime;

    private bool onGround;

    private Vector2 movementDirection;
    private Vector2 torchDirection;

    private Camera mainCam; //to avoid camera.main. . . transform or whatever;

    public InputActionReference move; //moving using 2d vector: WASD
    public InputActionReference sneakSprint; //sneak/sprint: ctrl, shift
    public InputActionReference rotate; //turning light source: Mouse Radius
    public InputActionReference jump; //jumping: space
    public InputActionReference dive; //diving: v
    public InputActionReference useItems; //use/equip: B
    public InputActionReference interact; //interact with env: F
    public InputActionReference traverseInv; //Left/Right: Q, E
    public InputActionReference manageInv; //Drop/Pickup: R, C
    public InputActionReference camZoom; //camZoom: T

    void Jump() { 
        if(!onGround) return;

        if(movementDirection.magnitude > 0.01f){ //negative forward vector as momentum cost for jumping
            rb.AddForce(rb.linearVelocity * (-playerSpeed * 0.8f) + Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else{   //if not moving, no negative vector
            rb.AddForce(rb.linearVelocity + Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void Dive() { //a bit too much horizontal force but better
        
        if(!onGround) return;

        if(Time.time < lastDiveTime + diveCooldown) return;

        lastDiveTime = Time.time; //notes when a dive is done
        
        /*idea is to apply a windup forward momentum (as if walking), a jump, and an impulse forward vector
        players dive in the direction they are moving, not the direction they are looking; unless stationary (reconsidering) */
        if(rb.linearVelocity.magnitude < 0.1f){ //if player is stationary
            Vector3 diveDirection = transform.forward * (diveForce * 2); 
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            rb.AddForce(diveDirection, ForceMode.VelocityChange);
        }
        else{
            Vector3 diveDirection = rb.linearVelocity * (diveForce / 2.5f) + Vector3.up * jumpForce; 
            rb.AddForce(diveDirection, ForceMode.VelocityChange);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        rb = GetComponent<Rigidbody>();
        mainCam = Camera.main;

        jump.action.performed += ctx => Jump();
        dive.action.performed += ctx => Dive();
    }

    // Update is called once per frame
    void Update() {
        movementDirection = move.action.ReadValue<Vector2>();
        torchDirection = rotate.action.ReadValue<Vector2>();
    }

    void FixedUpdate() {
        //rotation handling ; probably needs a quaternion but I am exhausted
        Vector3 playerScreen = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 torchLook = new Vector3(torchDirection.x, 0, torchDirection.y);
        Vector3 mousePosition = (torchLook - playerScreen).normalized;
        mousePosition.y = 0;
        transform.forward = mousePosition;

        //keyboard movement handling
        Vector3 velocity = rb.linearVelocity;
        velocity.x = movementDirection.x * playerSpeed;
        velocity.z = movementDirection.y * playerSpeed;

        rb.linearVelocity = velocity;

        //sprint/sneaking modifiers TODO

    }   

    void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Ground")) {
            onGround = true;
        }
    }

    void OnCollisionExit(Collision collision) {
        if (collision.gameObject.CompareTag("Ground")) {
            onGround = false;
        }
    }
}