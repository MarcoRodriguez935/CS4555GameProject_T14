using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    private Vector3 groundNormal = Vector3.up;
    public Rigidbody rb;
    public Texture2D cursorDet;
    private Vector2 cursorHotSpot = new Vector2(16,16);
    private CursorMode cursorMode = CursorMode.Auto;

    //player modifiers [items, status effects, defaults]
    private float playerSpeed = 3f;
    private float jumpForce = 3f;
    private float diveJump = 4.5f;
    private float diveSpeed = 10f;
    private float sneakMultiplier = .60f;
    private float sprintMultiplier = 2.5f;
    
    //cam/movement switches
    private float turnSpeedinDeg = 540f;
    private bool zoomed = false;
    private float detYawSensitivity = 220f;
    private float smoothYawVel;
    private float deltaSens = 0.0035f;
    private Vector2 aimAccum;
    private bool invertDetYaw = false;
    
    private float innerDeadzone = 0.05f; //turn after this
    private float outerDeadzone = 0.75f; //full input by here

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
        ApplyCursorState();

        jump.action.performed += ctx => Jump();
        dive.action.performed += ctx => Dive();
        sneak.action.performed += ctx => Sneak();
        sprint.action.performed += ctx => Sprint();
        interact.action.performed += ctx => Interact();
    }

    // Update is called once per frame
    void Update() {
        movementDirection = move.action.ReadValue<Vector2>();

        var aimRaw = rotate.action.ReadValue<Vector2>();
        var activeDev = rotate.action.activeControl != null ? rotate.action.activeControl.device : null;

        bool isMouse = activeDev is Mouse;

        if (isMouse) {
            // MOUSE DELTA → accumulate into a stable stick-like vector
            aimAccum += aimRaw * deltaSens;
            aimAccum = Vector2.ClampMagnitude(aimAccum, 1f);

            // (optional) very light recenter so it settles, but doesn't fight you
            if (aimRaw.sqrMagnitude < 0.0001f)
                aimAccum = Vector2.MoveTowards(aimAccum, Vector2.zero, 0.5f * Time.deltaTime);

            // apply radial deadzone so tiny motion = zero (kills jitter)
            torchDirection = RadialDeadzone(aimAccum, innerDeadzone, outerDeadzone);
        }
        else {
            // GAMEPAD STICK is already absolute [-1..1] → just deadzone & scale
            torchDirection = RadialDeadzone(aimRaw, innerDeadzone, outerDeadzone);
        }

        // torchDirection = rotate.action.ReadValue<Vector2>();
        // Vector2 delta = rotate.action.ReadValue<Vector2>();
        // aimAccum += delta * deltaSens;
        // aimAccum =  Vector2.ClampMagnitude(aimAccum, 1f);
        // torchDirection = aimAccum;
        // if (delta.sqrMagnitude < 3f) { //centers the cursor, prevents spinning when in detailed mode;
        //     aimAccum = Vector2.MoveTowards(aimAccum, Vector2.zero, 1.5f * Time.deltaTime);
        // }

        if(movementDirection.sqrMagnitude > 0.1f){ //saves the last movement direction when player is moving
            lastMoveDirection = new Vector3(movementDirection.x, 0f, movementDirection.y).normalized;
        }
    }

    void FixedUpdate() {
        // --------- GPT Rotation behavior ----------
        Vector3 camForward = mainCam.transform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = mainCam.transform.right; camRight.y = 0f; camRight.Normalize();
        
        Vector2 aimDirection = torchDirection;
        Vector3 aimWorld = camRight * aimDirection.x + camForward * aimDirection.y;
        float aimMag = aimDirection.magnitude;
        // float aimMag = new Vector2(aimDirection.x, aimDirection.y).magnitude;

        if (!zoomed) {
            if (aimMag > 0.0005f) { // prevent NaN/flip on near-zero
                Vector3 fwd = aimWorld.normalized;
                Quaternion targetRot = Quaternion.LookRotation(fwd, Vector3.up);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeedinDeg * Time.fixedDeltaTime));
            }
        } else {
            float yawInput = aimDirection.x * (invertDetYaw ? -1f : 1f);
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
    /* ----------------------- helper functions ------------------------- */

    void ApplyCursorState() {
        if (zoomed) {
            // DETAIL: show/enable cursor for interaction
            Cursor.lockState = CursorLockMode.Confined; // or None if you prefer
            Cursor.visible = true;
            if (cursorDet) Cursor.SetCursor(cursorDet, cursorHotSpot, cursorMode);
        } 
        else {
            // NAV: hide/lock
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }


    public void SetZoomedIn(bool value){
        zoomed = value;
        ApplyCursorState();
    }

    public bool IsZoomedIn() => zoomed;

    static Vector2 RadialDeadzone(Vector2 v, float inner, float outer){
        float m = v.magnitude;
        if (m <= inner) return Vector2.zero;
        float t = Mathf.InverseLerp(inner, outer, Mathf.Clamp01(m));
        return v.normalized * t;
    }
}