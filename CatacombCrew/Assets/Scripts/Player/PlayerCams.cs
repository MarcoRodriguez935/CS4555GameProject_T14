using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCams : MonoBehaviour
{
    public GameObject player;
    public Camera cam;
    private PlayerControl control;

    public InputActionReference camZoom;
    private bool defaultZoom = true;

    private float navFOV = 60f;
    private float detFOV = 45f;

    private Vector3 navOffset = new Vector3(0, 15, -7);
    private Vector3 detOffset = new Vector3(.5f, 2f, -1f);
    private Vector3 currentOffset;

    private Quaternion targetRotation;
    private Vector3 targetOffset;

    // --- NEW: Stable Yaw Handling ---
    private float smoothYaw;         // filtered yaw that ignores jitter
    private float yawSmoothSpeed = 6f; // higher = snappier, lower = smoother
    // -------------------------------

    void Start()
    {
        camZoom.action.performed += ctx => Zoom();
        control = player.GetComponent<PlayerControl>();

        cam.fieldOfView = navFOV;
        currentOffset = navOffset;
        targetOffset = navOffset;
        smoothYaw = player.transform.eulerAngles.y; // initialize
        targetRotation = Quaternion.Euler(65, smoothYaw, 0);

        control.SetZoomedIn(false);
    }

    void LateUpdate()
    {
        if (player == null) return;

        // --- Smooth out player's rotation to prevent jitter ---
        float targetYaw = player.transform.eulerAngles.y;
        smoothYaw = Mathf.LerpAngle(smoothYaw, targetYaw, Time.deltaTime * yawSmoothSpeed);
        Quaternion yawRotation = Quaternion.Euler(0f, smoothYaw, 0f);
        // -------------------------------------------------------

        if (!defaultZoom)
        {
            // DETAIL VIEW
            targetRotation = Quaternion.Euler(15f, smoothYaw, 0f);
            targetOffset = yawRotation * detOffset;
        }
        else
        {
            // NAV VIEW
            targetRotation = Quaternion.Euler(65f, smoothYaw, 0f);
            targetOffset = yawRotation * navOffset;
        }

        currentOffset = Vector3.Lerp(currentOffset, targetOffset, 5f * Time.deltaTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 180f * Time.deltaTime);
        transform.position = player.transform.position + currentOffset;
    }

    void Zoom()
    {
        if (defaultZoom)
        {
            cam.fieldOfView = detFOV;
            defaultZoom = false;
            control.SetZoomedIn(true);
        }
        else
        {
            cam.fieldOfView = navFOV;
            defaultZoom = true;
            control.SetZoomedIn(false);
        }
    }
}