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
    private float sneakMultiplier = .60f;
    private float sprintMultiplier = 2.5f;

    // Camera & rotation
    private float turnSpeedinDeg = 540f;
    private bool zoomed = false;
    private float detYawSensitivity = 220f;
    private float deltaSens = 0.0035f;
    private Vector2 aimAccum;
    private bool invertDetYaw = false;

    private float innerDeadzone = 0.05f;
    private float outerDeadzone = 0.75f;

    // Action cooldowns
    private float diveCooldown = 2.5f;
    private float lastDiveTime;

    // Player state
    private bool onGround;
    private bool onWalkable;
    public bool isSneaking = false;
    public bool isSprinting = false;

    // Movement direction
    public Vector2 movementDirection;
    private Vector2 torchDirection;
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

    // Stamina drain/regen
    private float staminaDrainRate = 25f; // per second
    private float staminaRegenRate = 15f; // per second

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCam = Camera.main;
        playerStats = GetComponent<PlayerStats>();
        ApplyCursorState();

        // Input event hooks
        jump.action.performed += ctx => Jump();
        dive.action.performed += ctx => Dive();
        sneak.action.performed += ctx => Sneak();
        interact.action.performed += ctx => Interact();
    }

    void Update()
    {
        movementDirection = move.action.ReadValue<Vector2>();

        // --- Sprint Logic ---
        bool sprintHeld = sprint.action.IsPressed();
        bool moving = movementDirection.sqrMagnitude > 0.1f;

        if (sprintHeld && moving && playerStats.CurrentStamina > 0f)
        {
            isSprinting = true;
            playerStats.UseStamina(staminaDrainRate * Time.deltaTime);
        }
        else
        {
            isSprinting = false;
            playerStats.RegainStamina(staminaRegenRate * Time.deltaTime);
        }
        // ---------------------

        // --- Rotation (camera-relative movement) ---
        var aimRaw = rotate.action.ReadValue<Vector2>();
        var activeDev = rotate.action.activeControl != null ? rotate.action.activeControl.device : null;
        bool isMouse = activeDev is Mouse;

        if (isMouse)
        {
            aimAccum += aimRaw * deltaSens;
            aimAccum = Vector2.ClampMagnitude(aimAccum, 1f);

            if (aimRaw.sqrMagnitude < 0.0001f)
                aimAccum = Vector2.MoveTowards(aimAccum, Vector2.zero, 0.5f * Time.deltaTime);

            torchDirection = RadialDeadzone(aimAccum, innerDeadzone, outerDeadzone);
        }
        else
        {
            torchDirection = RadialDeadzone(aimRaw, innerDeadzone, outerDeadzone);
        }

        if (movementDirection.sqrMagnitude > 0.1f)
            lastMoveDirection = new Vector3(movementDirection.x, 0f, movementDirection.y).normalized;
    }

    void FixedUpdate()
    {
        Vector3 camForward = mainCam.transform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = mainCam.transform.right; camRight.y = 0f; camRight.Normalize();

        Vector2 aimDirection = torchDirection;
        Vector3 aimWorld = camRight * aimDirection.x + camForward * aimDirection.y;
        float aimMag = aimDirection.magnitude;

        if (!zoomed)
        {
            if (aimMag > 0.0005f)
            {
                Vector3 fwd = aimWorld.normalized;
                Quaternion targetRot = Quaternion.LookRotation(fwd, Vector3.up);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeedinDeg * Time.fixedDeltaTime));
            }
        }
        else
        {
            float yawInput = aimDirection.x * (invertDetYaw ? -1f : 1f);
            if (Mathf.Abs(yawInput) > 0.01f)
            {
                float newYaw = transform.eulerAngles.y + yawInput * detYawSensitivity * Time.fixedDeltaTime;
                rb.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));
            }
        }

        // Stop rotation jitter on slopes
        if (onGround || onWalkable)
        {
            Vector3 angVel = rb.angularVelocity;
            angVel = Vector3.zero;
            rb.angularVelocity = new Vector3(0f, angVel.y, 0f);
        }

        // --- Movement Speed ---
        float currentSpeed = playerSpeed;
        if (isSneaking) currentSpeed *= sneakMultiplier;
        if (isSprinting) currentSpeed *= sprintMultiplier;

        Vector3 velocity = rb.linearVelocity;
        if (!zoomed)
        {
            velocity.x = movementDirection.x * currentSpeed;
            velocity.z = movementDirection.y * currentSpeed;
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }
        rb.linearVelocity = velocity;
    }

    // Collision checks
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

    // Actions
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

    // Helpers
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

    static Vector2 RadialDeadzone(Vector2 v, float inner, float outer)
    {
        float m = v.magnitude;
        if (m <= inner) return Vector2.zero;
        float t = Mathf.InverseLerp(inner, outer, Mathf.Clamp01(m));
        return v.normalized * t;
    }
}