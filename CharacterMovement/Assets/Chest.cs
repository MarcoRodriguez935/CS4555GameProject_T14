using UnityEngine;

public class Chest : MonoBehaviour
{
    [Header("References")]
    public Transform lid;
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;

    [Header("Settings")]
    public Vector3 openRotation = new Vector3(-75f, 0f, 0f); // lid rotation when open
    public float speed = 3f; // how fast lid opens

    private bool _playerInRange;
    private bool _isOpen = false;
    private Quaternion _closedRot;
    private Quaternion _openRot;

    void Start()
    {
        _closedRot = lid.localRotation;
        _openRot = Quaternion.Euler(openRotation);
    }

    void Update()
    {
        if (_playerInRange && Input.GetKeyDown(interactKey))
        {
            _isOpen = !_isOpen; // toggle
        }

        // Smoothly animate lid
        Quaternion target = _isOpen ? _openRot : _closedRot;
        lid.localRotation = Quaternion.Slerp(lid.localRotation, target, Time.deltaTime * speed);
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