using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{

    public Rigidbody rb;

    //player modifiers [items, status effects, defaults]
    private float playerSpeed = 3f;
    private float jumpForce = 3f;
    private float diveForce = 25f;
    private float sneakMultiplier = .60f;
    private float sprintMultiplier = 1.75f;
    // private float turnSpeedinDeg = 120f; //if we use a quaternion then it's for turn speed; might need one b/c controller and keyboard differences

    //action cooldowns [diving, item usage, status effects]
    private float diveCooldown = 2.5f;
    private float lastDiveTime;

    private float sprintCooldown = 8f;
    private float maxSprintTime = 2f;
    private float sprintEndTime;
    private float sprintAfterCool;


    //player state vars
    private bool onGround;
    private bool isSneaking = false;
    private bool isSprinting = false;

    //private float playerStamina = 100f;
    //private float playerSanity;

    private Vector2 movementDirection; //get the vector from wasd and convert to velocity for movement
    private Vector2 torchDirection; //get mouse position and convert to worldspace

    public Camera mainCam; //to avoid camera.main and do transforms

    //passthrough actions
    public InputActionReference move; //moving using 2d vector: WASD
    public InputActionReference rotate; //turning using 2d vector: mouse position, rightstick
    
    //button actions ; making sneaking/sprinting toggle for ease of controller implementation
    public InputActionReference jump; //jumping: space
    public InputActionReference dive; //diving: V
    public InputActionReference sneak; //sneak: ctrl
    public InputActionReference sprint; //sprint: shift
    //public InputActionReference interact; //interact with env: F

    /* cam, inventory actions should be placed in their own scripts for given system
    public InputActionReference useItems; //use/equip: B
    public InputActionReference traverseInv; //Left/Right: Q, E
    public InputActionReference manageInv; //Drop/Pickup: R, C
    public InputActionReference camZoom; //Zoom with Cam: T ----- used in the playercam script*/

    void Jump() { 
        if(!onGround) return;

        if(movementDirection.sqrMagnitude > 0.01f){ //negative forward vector as momentum cost for jumping
            rb.AddForce(rb.linearVelocity * (-playerSpeed * 0.8f) + Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else{   //if not moving, no negative vector
            rb.AddForce(rb.linearVelocity + Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void Dive() { //a bit floaty on the fall and teleporting feel moving forward
        
        if(!onGround) return;

        if(Time.time < lastDiveTime + diveCooldown) return;

        lastDiveTime = Time.time; //notes when a dive is done
        
        /*idea is to apply a windup forward momentum (as if walking), a jump, and an impulse forward vector
        players dive in the direction they are moving, not the direction they are looking; unless stationary (reconsidering) */
        if(rb.linearVelocity.sqrMagnitude < 0.1f){ //if player is stationary
            Vector3 diveDirection = transform.forward * (diveForce * 2); 
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            rb.AddForce(diveDirection, ForceMode.VelocityChange);
        }
        else{
            Vector3 diveDirection = rb.linearVelocity * (diveForce / 2.5f) + Vector3.up * jumpForce; 
            rb.AddForce(diveDirection, ForceMode.VelocityChange);   
        }

        //extraGravity at end of 
        if (rb.linearVelocity.y <= 0f) {
            Vector3 extraGrav = Physics.gravity * 3f;
            rb.AddForce(extraGrav, ForceMode.Acceleration);
        }
    }

    void Sneak(){
        isSneaking = !isSneaking;
        if(isSneaking) isSprinting = false;
    }

    void Sprint(){
        if(movementDirection.sqrMagnitude < 0.1f) return;

        if(isSprinting){ //end sprint early
            isSneaking = false;
            isSprinting = !isSprinting;
            sprintAfterCool = Time.time + sprintCooldown;
            return;
        }

        if(Time.time < sprintAfterCool) return; //check if sprint is available 

        isSprinting = true;
        isSneaking = false;
        sprintEndTime = Time.time + maxSprintTime;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        rb = GetComponent<Rigidbody>();
        mainCam = Camera.main;

        jump.action.performed += ctx => Jump();
        dive.action.performed += ctx => Dive();
        sneak.action.performed += ctx => Sneak();
        sprint.action.performed += ctx => Sprint();
    }

    // Update is called once per frame
    void Update() {
        movementDirection = move.action.ReadValue<Vector2>();
        torchDirection = rotate.action.ReadValue<Vector2>();
    }

    void FixedUpdate() {
        /*rotation handling ; probably needs a quaternion? maybe a raycast
        gonna need a rewrite for controller support though!*/
        Vector3 playerScreen = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 torchLook = new Vector3(torchDirection.x, 0, torchDirection.y);
        Vector3 mousePosition = (torchLook - playerScreen).normalized;
        mousePosition.y = 0;
        transform.forward = mousePosition;

        //apply sneak | sprint modifiers
        if(isSprinting && Time.time >= sprintEndTime){
            isSprinting = false; //sprint time is up
            sprintAfterCool = Time.time + sprintCooldown;
        } 
        
        float currentSpeed = playerSpeed;
        if(isSneaking) currentSpeed = playerSpeed * sneakMultiplier;
        if(isSprinting) currentSpeed = playerSpeed * sprintMultiplier;

        //keyboard movement handling
        Vector3 velocity = rb.linearVelocity;
        velocity.x = movementDirection.x * currentSpeed;
        velocity.z = movementDirection.y * currentSpeed;

        rb.linearVelocity = velocity;

    }   

    //i think there's a better way to handle collision checks for ground but I'll do it later
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