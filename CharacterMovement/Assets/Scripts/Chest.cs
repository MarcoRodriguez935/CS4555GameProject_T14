using UnityEngine;

public class Chest : MonoBehaviour
{
    [Header("References")]
    public Transform lidPivot; // drag your LidPivot here
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("Lid Settings")]
    public Vector3 openRotation = new Vector3(-75f, 0, 0); // local rotation for open state
    public float speed = 3f; // how fast lid opens/closes

    private bool playerInRange = false;
    private bool isOpen = false;
    private Quaternion closedRot;
    private Quaternion targetRot;

    void Start()
    {
        closedRot = lidPivot.localRotation;   // store starting closed rotation
        targetRot = closedRot;
    }

    void Update()
    {
        // toggle open/close if player presses interact key
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            isOpen = !isOpen;
            targetRot = isOpen ? Quaternion.Euler(openRotation) : closedRot;
        }

        // smoothly animate lid rotation
        lidPivot.localRotation = Quaternion.Slerp(lidPivot.localRotation, targetRot, Time.deltaTime * speed);
    }

    // detect player entering trigger area
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = true;
    }

    // detect player leaving trigger area
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = false;
    }
}