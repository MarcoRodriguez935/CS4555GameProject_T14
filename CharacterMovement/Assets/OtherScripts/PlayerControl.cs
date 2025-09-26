using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    private Vector3 groundNormal = Vector3.up;
    public Rigidbody rb;

    //player modifiers [items, status effects, defaults]
    private float playerSpeed = 3f;
    private float jumpForce = 3f;
    private float diveJump = 4.5f;
    private float diveSpeed = 10f;
    private float sneakMultiplier = .60f;
    private float sprintMultiplier = 2.5f;
    
    //cam/movement switches
    private float turnSpeedinDeg = 120f;
    private float aimDeadzone = 0.15f;
    private bool zoomed = false;
    private float detYawSensitivity = 220f;
    private float smoothYawVel;

    //action cooldowns [diving, item usage, status effects]
    private float diveCooldown = 2.5f;
    private float lastDiveTime;

    private float sprintCooldown = 5f;
    private float maxSprintTime = 2f;
    private float sprintEndTime;
    private float sprintAfterCool;

    //player state vars
    private bool onGround;
    private bool onWalkable;
    public bool isSneaking = false;
    public bool isSprinting = false;

    //private float playerStamina = 100f;
    //private float playerSanity;

    public Vector2 movementDirection; //get the vector from wasd and convert to velocity for movement
    private Vector2 torchDirection; //get mouse position and convert to worldspace
    private Vector3 lastMoveDirection = Vector3.forward; //to save last direction for dives.

    public Camera mainCam; //to avoid camera.main and do transforms

    //passthrough actions
    public InputActionReference move; //moving using 2d vector: WASD
    public InputActionReference rotate; //turning using 2d vector: mouse position, rightstick
    
    //button actions ; making sneaking/sprinting toggle for ease of controller implementation
    public InputActionReference jump; //jumping: space
    public InputActionReference dive; //diving: V
    public InputActionReference sneak; //sneak: ctrl
    public InputActionReference sprint; //sprint: shift
    public InputActionReference interact; //interact with env: F

    /* cam, inventory actions should be placed in their own scripts for given system
    public InputActionReference useItems; //use/equip: B
    public InputActionReference traverseInv; //Left/Right: Q, E
    public InputActionReference manageInv; //Drop/Pickup: R, C
    public InputActionReference camZoom; //Zoom with Cam: T ----- used in the playercam script*/

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        rb = GetComponent<Rigidbody>();
        mainCam = Camera.main;

        jump.action.performed += ctx => Jump();
        dive.action.performed += ctx => Dive();
        sneak.action.performed += ctx => Sneak();
        sprint.action.performed += ctx => Sprint();
        interact.action.performed += ctx => Interact();
    }

    // Update is called once per frame
    void Update() {
        movementDirection = move.action.ReadValue<Vector2>();
        torchDirection = rotate.action.ReadValue<Vector2>();
        
        if(movementDirection.sqrMagnitude > 0.1f){ //saves the last movement direction when player is moving
            lastMoveDirection = new Vector3(movementDirection.x, 0f, movementDirection.y).normalized;
        }
    }

    void FixedUpdate() {
        // --------- GPT Rotation behavior ----------
        Vector3 camForward = mainCam.transform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = mainCam.transform.right; camRight.y = 0f; camRight.Normalize();
        Vector2 aimDirection = torchDirection;
        Vector3 aimInWorld = camRight * aimDirection.x + camForward * aimDirection.y;
        float aimMag = new Vector2(aimDirection.x, aimDirection.y).magnitude;
        if (!zoomed) {
            // NAV mode: character faces aim vector when outside deadzone.
            if (aimMag > aimDeadzone) {
                Vector3 fwd = aimInWorld.sqrMagnitude > 1e-6f ? aimInWorld.normalized : transform.forward;
                float targetYaw = Quaternion.LookRotation(fwd, Vector3.up).eulerAngles.y;
                float newYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref smoothYawVel, 0.08f, turnSpeedinDeg, Time.fixedDeltaTime);
                rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
            }
        } else {
            // DET mode: no locomotion; rotate by yaw input (mouse X / stick X)
            float yawInput = aimDirection.x; // left/right on stick or mouse X if you mapped it
            if (Mathf.Abs(yawInput) > 0.01f) {
                float newYaw = transform.eulerAngles.y + yawInput * detYawSensitivity * Time.fixedDeltaTime;
                rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
            }
        }

        // /*rotation handling ; probably needs a quaternion? maybe a raycast
        // gonna need a rewrite for controller support though!*/
        // Vector3 torchLook = new Vector3(torchDirection.x, 0, torchDirection.y);

        // //"viewing vector is zero" block
        // if(torchLook.sqrMagnitude > 1e-6f){
        //     rb.MoveRotation(Quaternion.LookRotation(torchLook));
        // }

        //to fix jittering when player collides or dives on edges/objects
        if(onGround || onWalkable){
            Vector3 angVel = rb.angularVelocity;
            angVel = Vector3.zero;
            rb.angularVelocity = new Vector3(0f, angVel.y, 0f);
        }

        //apply sneak | sprint modifiers
        if (isSprinting && Time.time >= sprintEndTime) {
            isSprinting = false;
            sprintAfterCool = Time.time + sprintCooldown;
        }

        float currentSpeed = playerSpeed;
        if (isSneaking) currentSpeed = playerSpeed * sneakMultiplier;
        if (isSprinting) currentSpeed = playerSpeed * sprintMultiplier;

        //keyboard movement handling
        Vector3 velocity = rb.linearVelocity;
        if(!zoomed){
            velocity.x = movementDirection.x * currentSpeed;
            velocity.z = movementDirection.y * currentSpeed;
        }
        else{
            velocity.x = 0f;
            velocity.z = 0f;
        }
        rb.linearVelocity = velocity;  
    }

    /* ----------------------------- Collision Handling ------------------------------- */

    //i think there's a better way to handle collision checks for ground but I'll do it later
    void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Ground")) {
            onGround = true;
        }
        if(collision.gameObject.CompareTag("Walkable")) {
            onWalkable = true;
            onGround = false;
        }
        
        if(collision.gameObject.CompareTag("Trap")){
            Transform trap = collision.collider.transform;
            Vector3 trapKnockback = rb.position - trap.position;
            trapKnockback.y = 0f;
            if(trapKnockback.sqrMagnitude < 1e-4f) trapKnockback = -transform.forward;
            trapKnockback.Normalize();

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(trapKnockback * -5f + Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    void OnCollisionExit(Collision collision) {
        if (collision.gameObject.CompareTag("Ground")) {
            onGround = false;
        }
        if(collision.gameObject.CompareTag("Walkable")) {
            onWalkable = false;
        }
    }

   /* ----------------------------- Player Actions ------------------------------- */

    void Jump() { 
        if(!onGround && !onWalkable) return;

        if(movementDirection.sqrMagnitude > 0.01f){ //negative forward vector as momentum cost for jumping
            rb.AddForce(rb.linearVelocity * (-playerSpeed * 0.8f) + Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else{   //if not moving, no negative vector
            rb.AddForce(rb.linearVelocity + Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void Dive() { //a bit floaty on the fall and teleporting feel moving forward
        
        if(!onGround && !onWalkable) return;

        if(Time.time < lastDiveTime + diveCooldown) return;

        lastDiveTime = Time.time; //notes when a dive was last used

        Vector3 stationaryDiveDir = lastMoveDirection;
        
        /*
        idea is to apply a windup forward momentum (as if walking), a jump, and an impulse forward vector
        players dive in the direction they are moving, not the direction they are looking; unless stationary (reconsidering) 
        ts pmo, the dive is good when the player holds the movement key after moving, otherwise its pretty bad
        */
        if(rb.linearVelocity.sqrMagnitude < 0.1f){ //if player is stationary
            Vector3 diveDirection = stationaryDiveDir * diveSpeed + Vector3.up * diveJump; 
            rb.AddForce(diveDirection, ForceMode.Impulse);

        }
        else{
            Vector3 diveDirection = rb.linearVelocity * diveSpeed + Vector3.up * diveJump; 
            rb.AddForce(diveDirection, ForceMode.Impulse);   
        }

        //extraGravity at end of dive action
        if (rb.linearVelocity.y <= 0f) {
            Vector3 extraGrav = Physics.gravity * 5f;
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

    void Interact(){
        //interact code TODO
    }
    public void SetZoomedIn(bool value) => zoomed = value;
    public bool IsZoomedIn() => zoomed;
}