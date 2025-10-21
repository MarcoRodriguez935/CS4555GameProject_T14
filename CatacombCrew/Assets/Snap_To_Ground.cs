using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SnapToGround : MonoBehaviour
{
    public float rayDistance = 100f;   // How far to look down for ground
    public float offset = 0.05f;       // Small offset above the ground
    public LayerMask groundMask;       // Optional: assign “Ground” layer

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Start from the player's center and cast downward
        Vector3 rayStart = transform.position + Vector3.up * 1f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundMask))
        {
            // Position player so the CharacterController capsule just touches ground
            Vector3 newPos = transform.position;
            newPos.y = hit.point.y + controller.height / 2f - controller.center.y + offset;
            transform.position = newPos;
        }
        else
        {
            Debug.LogWarning($"{name} could not find ground below within {rayDistance} units.");
        }
    }
}