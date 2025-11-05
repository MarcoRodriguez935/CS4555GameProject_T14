using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    private Vector3 groundNormal = Vector3.up;
    public Rigidbody rb;
    public Texture2D cursorDet;
    private Vector2 cursorHotSpot = new Vector2(16, 16);
    private CursorMode cursorMode = CursorMode.Auto;

    // Player references
    private PlayerStats playerStats;

    // Movement
    private float playerSpeed = 3f;
    private float jumpForce = 3f;
    private float diveJump = 4.5f;
    private float diveSpeed = 10f;
    private float sneakMultiplier = 0.6f;
    private float sprintMultiplier = 2.5f;

    // Camera / rotation
    private float turnSpeedinDeg = 180f;
    private bool zoomed = false;

    // Cooldowns
    private float diveCooldown = 2.5f;
    private float lastDiveTime;

    // Player states
    private bool onGround;
    private bool onWalkable;
    public bool isSneaking = false;
    public bool isSprinting = false;

    // Movement
    public Vector2 movementDirection;
    private Vector3 lastMoveDirection = Vector3.forward;

    // Camera
    public Camera mainCam;

    // Input Actions
    public InputActionReference move;
    public InputActionReference rotate;
    public InputActionReference jump;
    public InputActionReference dive;
    public InputActionReference sneak;
    public InputActionReference sprint;
    public InputActionReference interact;

    // Stamina logic
    private float staminaDrainRate = 25f;
    private float staminaRegenRate = 15f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCam = Camera.main;
        playerStats = GetComponent<PlayerStats>();

        // Freeze physics rotation
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationY |
                             RigidbodyConstraints.FreezeRotationZ;
        }

        // ✅ Enable this player’s action map
        EnableActionMap(move);
        EnableActionMap(rotate);
        EnableActionMap(sprint);
        EnableActionMap(jump);
        EnableActionMap(dive);
        EnableActionMap(sneak);
        EnableActionMap(interact);

        ApplyCursorState();

        // Input event hooks
        if (jump != null) jump.action.performed += ctx => Jump();
        if (dive != null) dive.action.performed += ctx => Dive();
        if (sneak != null) sneak.action.performed += ctx => Sneak();
        if (interact != null) interact.action.performed += ctx => Interact();
    }

    private void EnableActionMap(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null && actionRef.action.actionMap != null)
            actionRef.action.actionMap.Enable();
    }

    void Update()
    {
        // --- Movement input ---
        movementDirection = move != null ? move.action.ReadValue<Vector2>() : Vector2.zero;
        bool sprintHeld = sprint != null && sprint.action.IsPressed();
        bool moving = movementDirection.sqrMagnitude > 0.1f;

        // --- Sprint logic ---
        if (sprintHeld && moving && playerStats.CurrentStamina > 0f)
        {
            isSprinting = true;
            playerStats.UseStamina(staminaDrainRate * Time.deltaTime);

            if (playerStats.CurrentStamina <= 0f)
                isSprinting = false;
        }
        else
        {
            isSprinting = false;
            playerStats.RegainStamina(staminaRegenRate * Time.deltaTime);
        }

        // --- Rotation (Z/C or N/,) ---
        float rotateInput = rotate != null ? rotate.action.ReadValue<float>() : 0f;
        if (Mathf.Abs(rotateInput) > 0.01f)
        {
            float newYaw = transform.eulerAngles.y + rotateInput * turnSpeedinDeg * Time.deltaTime;
            rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
        }

        // Save last move direction
        if (movementDirection.sqrMagnitude > 0.1f)
            lastMoveDirection = new Vector3(movementDirection.x, 0f, movementDirection.y).normalized;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // --- Player-relative movement (NOT camera-relative anymore) ---
        Vector3 inputDir = new Vector3(movementDirection.x, 0f, movementDirection.y);
        Vector3 moveDir = transform.TransformDirection(inputDir);

        float currentSpeed = playerSpeed;
        if (isSneaking) currentSpeed *= sneakMultiplier;
        if (isSprinting) currentSpeed *= sprintMultiplier;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDir.x * currentSpeed;
        velocity.z = moveDir.z * currentSpeed;

        rb.linearVelocity = velocity;
    }

    // --- Collisions ---
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) onGround = true;
        if (collision.gameObject.CompareTag("Walkable"))
        {
            onWalkable = true;
            onGround = false;
        }

        if (collision.gameObject.CompareTag("Trap"))
        {
            Transform trap = collision.collider.transform;
            Vector3 trapKnockback = rb.position - trap.position;
            trapKnockback.y = 0f;
            if (trapKnockback.sqrMagnitude < 1e-4f) trapKnockback = -transform.forward;
            trapKnockback.Normalize();

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(trapKnockback * -5f + Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) onGround = false;
        if (collision.gameObject.CompareTag("Walkable")) onWalkable = false;
    }

    // --- Player Actions ---
    void Jump()
    {
        if (!onGround && !onWalkable) return;

        if (movementDirection.sqrMagnitude > 0.01f)
            rb.AddForce(rb.linearVelocity * (-playerSpeed * 0.8f) + Vector3.up * jumpForce, ForceMode.Impulse);
        else
            rb.AddForce(rb.linearVelocity + Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void Dive()
    {
        if (!onGround && !onWalkable) return;
        if (Time.time < lastDiveTime + diveCooldown) return;
        lastDiveTime = Time.time;

        Vector3 stationaryDiveDir = lastMoveDirection;

        if (rb.linearVelocity.sqrMagnitude < 0.1f)
            rb.AddForce(stationaryDiveDir * diveSpeed + Vector3.up * diveJump, ForceMode.Impulse);
        else
            rb.AddForce(rb.linearVelocity * diveSpeed + Vector3.up * diveJump, ForceMode.Impulse);

        if (rb.linearVelocity.y <= 0f)
        {
            Vector3 extraGrav = Physics.gravity * 5f;
            rb.AddForce(extraGrav, ForceMode.Acceleration);
        }
    }

    void Sneak()
    {
        isSneaking = !isSneaking;
        if (isSneaking) isSprinting = false;
    }

    void Interact() { }

    // --- Helpers ---
    void ApplyCursorState()
    {
        if (zoomed)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            if (cursorDet) Cursor.SetCursor(cursorDet, cursorHotSpot, cursorMode);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void SetZoomedIn(bool value)
    {
        zoomed = value;
        ApplyCursorState();
    }

    public bool IsZoomedIn() => zoomed;
}