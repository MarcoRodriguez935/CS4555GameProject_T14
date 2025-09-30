using UnityEngine;

public class Lever : MonoBehaviour
{
    [Header("Interaction")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    public DoorController door;

    [Header("Lever visuals (optional)")]
    public Transform handle; 
    public Vector3 onLocalEuler = new Vector3(-45f, 0f, 0f);
    public Vector3 offLocalEuler = Vector3.zero;

    bool _playerInRange;

    void Start()
    {
        if (handle != null) handle.localEulerAngles = offLocalEuler;
    }

    void Update()
    {
        if (_playerInRange && Input.GetKeyDown(interactKey))
        {
            door?.ToggleDoor();

            if (handle != null)
            {
                // simple toggle of the lever handle
                if (Vector3.Distance(handle.localEulerAngles, offLocalEuler) < 1f)
                    handle.localEulerAngles = onLocalEuler;
                else
                    handle.localEulerAngles = offLocalEuler;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerInRange = false;
    }
}